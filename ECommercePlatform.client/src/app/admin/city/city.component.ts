import { Component, OnInit, OnDestroy, ChangeDetectorRef } from '@angular/core';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule, FormsModule, ValidatorFn, AbstractControl } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { City } from '../../models/city.model';
import { State } from '../../models/state.model';
import { Country } from '../../models/country.model';
import { CityService } from '../../services/city/city.service';
import { StateService } from '../../services/state/state.service';
import { CountryService } from '../../services/country/country.service';
import { AuthService } from '../../services/auth/auth.service';
import { MessageService, Message } from '../../services/general/message.service';
import { Subscription } from 'rxjs';
import { CustomValidatorsService } from '../../services/custom-validators/custom-validators.service';
import { PaginationComponent } from '../../shared/pagination/pagination.component';
import { PagedResponse, PagedRequest } from '../../models/pagination.model';
import { ListService } from '../../services/general/list.service';
import { DateFilterService, DateRange } from '../../services/general/date-filter.service';
import { DateRangeFilterComponent } from '../../shared/date-range-filter/date-range-filter.component';
//import { PermissionDirective } from '../../directives/permission.directive';
import { PermissionType } from '../../models/role.model';
import { AuthorizationService } from '../../services/authorization/authorization.service';

@Component({
  selector: 'app-city',
  templateUrl: './city.component.html',
  styleUrls: ['./city.component.scss'],
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule, FormsModule, PaginationComponent, DateRangeFilterComponent]
})

export class CityComponent implements OnInit, OnDestroy {
  cities: City[] = [];
  states: State[] = [];
  countries: Country[] = [];
  cityForm!: FormGroup;
  isEditMode: boolean = false;
  currentCityId: string | null = null;
  loading: boolean = false;
  selectedCountryId: string = '';
  message: Message | null = null;
  private currentUser: any = null;
  private messageSubscription!: Subscription;
  private searchSubscription!: Subscription;
  private dateRangeSubscription!: Subscription;
  private permissionSubscription!: Subscription;
  Math = Math;
  selectedStateId = 'all';
  selectedCountryIdFilter: string = 'all';
  filteredStates: State[] = [];
  allStates: State[] = [];
  PermissionType = PermissionType;

  // Permission-dependent UI state
  canAddEdit: boolean = false;
  canDelete: boolean = false;
  canView: boolean = false;

  // Pagination properties
  pagedResponse: PagedResponse<City> | null = null;
  pageRequest: PagedRequest = {
    pageNumber: 1,
    pageSize: 10,
    searchText: '',
    sortColumn: 'name',
    sortDirection: 'asc'
  };
    
  constructor(
    private cityService: CityService,
    private stateService: StateService,
    private countryService: CountryService,
    private authService: AuthService,
    public authorizationService: AuthorizationService,
    private messageService: MessageService,
    private listService: ListService,
    private dateFilterService: DateFilterService,
    private fb: FormBuilder,
    private cdr: ChangeDetectorRef
  ) { }

  ngOnInit(): void {
    this.initForm();
    this.loadCountries();
    this.loadAllStates();
    this.loadCities();

    // Initial permission check
    this.checkPermissions();

    // Subscribe to global permission changes
    this.permissionSubscription = this.authorizationService.globalPermissionChange$.subscribe(() => {
      this.checkPermissions(); // Refresh permission-dependent state
      this.cdr.detectChanges(); // Force change detection
    });

    this.authService.currentUser$.subscribe(user => {
      this.currentUser = user;
    });

    // Subscribe to message changes
    this.messageSubscription = this.messageService.currentMessage.subscribe(message => {
      this.message = message;
    });

    // Subscribe to search changes with debounce
    this.searchSubscription = this.listService.getSearchObservable().subscribe(term => {
      this.pageRequest.searchText = term;
      this.pageRequest.pageNumber = 1;
      this.loadCities();
    });

    // Subscribe to date range changes
    this.dateRangeSubscription = this.dateFilterService.getDateRangeObservable()
      .subscribe((dateRange: DateRange) => {
        this.pageRequest.startDate = dateRange.startDate ? dateRange.startDate.toISOString() : null;
        this.pageRequest.endDate = dateRange.endDate ? dateRange.endDate.toISOString() : null;
        this.pageRequest.pageNumber = 1;
        this.loadCities();
      });
  }

  ngOnDestroy(): void {
    // Clean up subscriptions
    if (this.messageSubscription) {
      this.messageSubscription.unsubscribe();
    }
    if (this.searchSubscription) {
      this.searchSubscription.unsubscribe();
    }
    if (this.dateRangeSubscription) {
      this.dateRangeSubscription.unsubscribe();
    }
    if (this.permissionSubscription) {
      this.permissionSubscription.unsubscribe();
    }
  }

  // method to check permissions
  private checkPermissions(): void {
    const wasViewable = this.canView;

    this.canView = this.authorizationService.hasPermission('cities', PermissionType.View);
    this.canAddEdit = this.authorizationService.hasPermission('cities', PermissionType.AddEdit);
    this.canDelete = this.authorizationService.hasPermission('cities', PermissionType.Delete);

    // If view permission was lost, redirect immediately
    if (wasViewable && !this.canView && !this.authorizationService.isAdmin()) {
      this.authorizationService.checkAndRedirectOnPermissionLoss('cities', PermissionType.View);
    }

    console.log('Cities permissions updated:', { canView: this.canView, canAddEdit: this.canAddEdit, canDelete: this.canDelete });
  }

  private initForm(): void {
    this.cityForm = this.fb.group({
      name: ['', [Validators.required, Validators.maxLength(100), CustomValidatorsService.noWhitespaceValidator(), CustomValidatorsService.lettersOnly()]],
      stateId: ['', [Validators.required]],
      isActive: [true]
    });
  }

  loadAllStates(): void {
    this.stateService.getStates().subscribe({
      next: (states) => {
        this.allStates = states;
        this.filteredStates = states; // Initially all states are shown
      },
      error: (error) => {
        console.error('Error loading states:', error);
        const errorMessage = error.error?.message ||
          error.error?.title ||
          error.message ||
          'Failed to load states';
        this.messageService.showMessage({ type: 'error', text: errorMessage });
      }
    });
  }

  loadCountries(): void {
    this.countryService.getCountries().subscribe({
      next: (countries) => {
        this.countries = countries;
      },
      error: (error) => {
        console.error('Error loading countries:', error);
        const errorMessage = error.error?.message ||
          error.error?.title ||
          error.message ||
          'Failed to load countries';
        this.messageService.showMessage({ type: 'error', text: errorMessage });
      }
    });
  }

  loadStates(countryId: string = ''): void {
    if (countryId) {
      this.stateService.getStatesByCountry(countryId).subscribe({
        next: (states) => {
          this.states = states;
          // Reset state selection in form when country changes
          this.cityForm.get('stateId')?.setValue('');
        },
        error: (error) => {
          console.error('Error loading states by country:', error);
          const errorMessage = error.error?.message ||
            error.error?.title ||
            error.message ||
            'Failed to load states for the selected country';
          this.messageService.showMessage({ type: 'error', text: errorMessage });
        }
      });
    } else {
      this.stateService.getStates().subscribe({
        next: (states) => {
          this.states = states;
        },
        error: (error) => {
          console.error('Error loading states:', error);
          const errorMessage = error.error?.message ||
            error.error?.title ||
            error.message ||
            'Failed to load states';
          this.messageService.showMessage({ type: 'error', text: errorMessage });
        }
      });
    }
  }

  loadCities(): void {
    this.loading = true;
    const stateId = this.selectedStateId === 'all' ? undefined : this.selectedStateId;
    let countryId = this.selectedCountryIdFilter === 'all' ? undefined : this.selectedCountryIdFilter;

    // If a specific state is selected, we don't need to filter by country since states belong to one country
    if (stateId) {
      countryId = undefined;
    }

    this.cityService.getPagedCities(this.pageRequest, stateId, countryId, false).subscribe({
      next: (response) => {
        this.cities = response.items;
        this.pagedResponse = response;
        this.loading = false;
      },
      error: (error) => {
        console.error('Error loading cities:', error);
        const errorMessage = error.error?.message ||
          error.error?.title ||
          error.message ||
          'Failed to load cities';
        this.messageService.showMessage({ type: 'error', text: errorMessage });
        this.loading = false;
      }
    });
  }

  onSubmit(): void {
    if (this.cityForm.invalid) {
      return;
    }

    const cityData: City = {
      ...this.cityForm.value,
      id: this.isEditMode && this.currentCityId ? this.currentCityId : undefined,
      createdOn: new Date(),
      createdBy: this.isEditMode ? undefined : this.getUserIdentifier(),
      modifiedOn: new Date(),
      modifiedBy: this.getUserIdentifier(),
      //isActive: true,
      isDeleted: false
    };

    this.loading = true;

    if (this.isEditMode && this.currentCityId) {
      this.cityService.updateCity(this.currentCityId, cityData).subscribe({
        next: () => {
          this.messageService.showMessage({ type: 'success', text: 'City updated successfully' });
          this.loadCities();
          this.resetForm();
          this.loading = false;
        },
        error: (error) => {
          console.error('Error updating city:', error);
          // Extract the most useful error message
          const errorMessage = error.error?.message ||
            error.error?.title ||
            error.message ||
            'Failed to update City';
          this.messageService.showMessage({ type: 'error', text: errorMessage });
          this.loading = false;
        }
      });
    } else {
      this.cityService.createCity(cityData).subscribe({
        next: () => {
          this.messageService.showMessage({ type: 'success', text: 'City created successfully' });
          this.loadCities();
          this.resetForm();
          this.loading = false;
        },
        error: (error) => {
          console.error('Error creating city:', error);
          const errorMessage = error.error?.message ||
            error.error?.title ||
            error.message ||
            'Failed to create City';
          this.messageService.showMessage({ type: 'error', text: errorMessage });
          this.loading = false;
        }
      });
    }
  }

  editCity(city: City): void {
    this.isEditMode = true;
    this.currentCityId = city.id || null;

    // Always scroll to top immediately when edit button is clicked
    this.messageService.scrollToTop();


    // Find the state to get the associated country
    const state = this.states.find(s => s.id === city.stateId);
    if (state) {
      this.selectedCountryId = state.countryId;

      // First load states for the selected country
      this.stateService.getStatesByCountry(state.countryId).subscribe({
        next: (states) => {
          this.states = states;

          // After states are loaded, set form values
          setTimeout(() => {
            this.cityForm.patchValue({
              name: city.name,
              stateId: city.stateId,
              isActive: city.isActive !== undefined ? city.isActive : true
            });
          });
        },
        error: (error) => {
          console.error('Error loading states for city editing:', error);
          const errorMessage = error.error?.message ||
            error.error?.title ||
            error.message ||
            'Failed to load states for the selected country';
          this.messageService.showMessage({ type: 'error', text: errorMessage });
        }
      });
    } else {
      // If can't find the state in current list,
      // need to load all states first to ensure it has the right data
      this.stateService.getStates().subscribe({
        next: (states) => {
          this.states = states;

          // Now try to find the state again with the fresh data
          const refreshedState = this.states.find(s => s.id === city.stateId);
          if (refreshedState) {
            this.selectedCountryId = refreshedState.countryId;

            // Load states for this country
            this.stateService.getStatesByCountry(refreshedState.countryId).subscribe({
              next: (countryStates) => {
                this.states = countryStates;

                // Now set form values
                setTimeout(() => {
                  this.cityForm.patchValue({
                    name: city.name,
                    stateId: city.stateId,
                    isActive: city.isActive !== undefined ? city.isActive : true
                  });
                });
              }
            });
          } else {
            // If still can't find state, just patch form with what we have
            this.cityForm.patchValue({
              name: city.name,
              stateId: city.stateId,
              isActive: city.isActive !== undefined ? city.isActive : true
            });
          }
        },
        error: (error) => {
          console.error('Error loading all states for city editing:', error);
          const errorMessage = error.error?.message ||
            error.error?.title ||
            error.message ||
            'Failed to load states';
          this.messageService.showMessage({ type: 'error', text: errorMessage });
        }
      });
    }
  }

  deleteCity(id: string): void {
    if (confirm('Are you sure you want to delete this city?')) {
      this.loading = true;
      this.cityService.deleteCity(id).subscribe({
        next: () => {
          this.messageService.showMessage({ type: 'success', text: 'City deleted successfully' });
          this.loadCities();
          this.loading = false;
        },
        error: (error) => {
          console.error('Error deleting city:', error);
          const errorMessage = error.error?.message ||
            error.error?.title ||
            error.message ||
            'Failed to delete City';
          this.messageService.showMessage({ type: 'error', text: errorMessage });
          this.loading = false;
        }
      });
    }
  }

  resetForm(): void {
    this.cityForm.reset({ isActive: true });
    this.isEditMode = false;
    this.currentCityId = null;
    this.selectedCountryId = '';
  }

  toggleStatus(city: City): void {
    if (!city.id || !this.authorizationService.hasPermission('cities', PermissionType.AddEdit)) return;

    this.loading = true;

    // Create an update object with all required fields
    const update = {
      id: city.id,
      name: city.name,        
      stateId: city.stateId,  
      isActive: !city.isActive,
      modifiedBy: this.getUserIdentifier(),
      modifiedOn: new Date()
    };

    this.cityService.updateCity(city.id, update).subscribe({
      next: () => {
        // Update the item in the local array without reloading
        city.isActive = !city.isActive;
        this.messageService.showMessage({
          type: 'success',
          text: `City ${city.isActive ? 'activated' : 'deactivated'} successfully`
        });
        this.loadCities();
        this.loading = false;
      },
      error: (error) => {
        console.error('Error updating status:', error);
        const errorMessage = error.error?.message || 'Failed to update status';
        this.messageService.showMessage({ type: 'error', text: errorMessage });
        this.loading = false;
      }
    });
  }

  getStateName(stateId: string): string {
    const state = this.allStates.find(s => s.id === stateId);
    return state ? state.name : 'Unknown';
  }

  getCountryForState(stateId: string): string {
    const state = this.allStates.find(s => s.id === stateId);
    if (!state) return 'Unknown';

    const country = this.countries.find(c => c.id === state.countryId);
    return country ? country.name : 'Unknown';
  }

  onStateFilterChange(event: Event): void {
    const select = event.target as HTMLSelectElement;
    this.filterCitiesByState(select.value);
  }

  filterCitiesByState(stateId: string): void {
    this.selectedStateId = stateId;
    this.pageRequest.pageNumber = 1; // Reset to first page when filter changes
    this.loadCities();
  }

  onCountryChange(event: Event): void {
    const select = event.target as HTMLSelectElement;
    const countryId = select.value;
    this.selectedCountryId = countryId;
    if (countryId) {
      this.loadStates(countryId);
      this.cityForm.get('stateId')?.enable(); // Enable the state dropdown
    } else {
      this.states = [];
      this.cityForm.get('stateId')?.setValue('');
      this.cityForm.get('stateId')?.disable(); // Disable the state dropdown
    }
  }

  // Add country filter change handler
  onCountryFilterChange(event: Event): void {
    const select = event.target as HTMLSelectElement;
    this.filterCitiesByCountry(select.value);
  }

  // Add method to filter cities by country
  filterCitiesByCountry(countryId: string): void {
    this.selectedCountryIdFilter = countryId;

    // Reset state filter when country changes
    this.selectedStateId = 'all';

    // Update the states dropdown based on selected country
    if (countryId === 'all') {
      this.filteredStates = this.allStates;
    } else {
      this.filteredStates = this.allStates.filter(state => state.countryId === countryId);
    }

    // Reset to first page when filter changes
    this.pageRequest.pageNumber = 1;

    // Load cities with the new filters
    this.loadCities();
  }

  // Helper method to get user identifier for creation/modification tracking
  private getUserIdentifier(): string {
    return this.currentUser ? this.currentUser.id || this.currentUser.email : 'system';
  }

  // Pagination methods
  onPageChange(page: number): void {
    this.pageRequest.pageNumber = page;
    this.loadCities();
  }

  onPageSizeChange(event: Event): void {
    const selectElement = event.target as HTMLSelectElement;
    this.pageRequest.pageSize = +selectElement.value;
    this.pageRequest.pageNumber = 1; // Reset to first page when changing page size
    this.loadCities();
  }

  onSortChange(column: string): void {
    // If clicking the same column, toggle direction
    if (this.pageRequest.sortColumn === column) {
      this.pageRequest.sortDirection =
        this.pageRequest.sortDirection === 'asc' ? 'desc' : 'asc';
    } else {
      this.pageRequest.sortColumn = column;
      this.pageRequest.sortDirection = 'asc';
    }
    this.loadCities();
  }

  onSearch(event: Event): void {
    const searchTerm = (event.target as HTMLInputElement).value;
    this.listService.search(searchTerm);
  }
}
