import { Component, OnInit, OnDestroy, ChangeDetectorRef } from '@angular/core';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule, FormsModule, ValidatorFn, AbstractControl } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { State } from '../../models/state.model';
import { Country } from '../../models/country.model';
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
  selector: 'app-state',
  templateUrl: './state.component.html',
  styleUrls: ['./state.component.scss'],
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule, FormsModule, PaginationComponent, DateRangeFilterComponent]
})
export class StateComponent implements OnInit, OnDestroy {
  states: State[] = [];
  countries: Country[] = [];
  stateForm!: FormGroup;
  isEditMode: boolean = false;
  currentStateId: string | null = null;
  loading: boolean = false;
  message: Message | null = null;
  private currentUser: any = null;
  private messageSubscription!: Subscription;
  private searchSubscription!: Subscription;
  private dateRangeSubscription!: Subscription;
  private permissionSubscription!: Subscription;
  Math = Math;
  selectedCountryId = 'all';
  PermissionType = PermissionType;

  // Permission-dependent UI state
  canAddEdit: boolean = false;
  canDelete: boolean = false;
  canView: boolean = false;

  // Pagination properties
  pagedResponse: PagedResponse<State> | null = null;
  pageRequest: PagedRequest = {
    pageNumber: 1,
    pageSize: 10,
    searchText: '',
    sortColumn: 'name',
    sortDirection: 'asc'
  };

  constructor(
    private stateService: StateService,
    private countryService: CountryService,
    private authService: AuthService,
    private messageService: MessageService,
    private listService: ListService,
    private dateFilterService: DateFilterService,
    public authorizationService: AuthorizationService,
    private fb: FormBuilder,
    private cdr: ChangeDetectorRef
  ) { }

  ngOnInit(): void {
    this.initForm();
    this.loadCountries();
    this.loadStates();

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
      this.pageRequest.pageNumber = 1; // Reset to first page when search changes
      this.loadStates();
    });

    // Subscribe to date range changes
    this.dateRangeSubscription = this.dateFilterService.getDateRangeObservable()
      .subscribe((dateRange: DateRange) => {
        this.pageRequest.startDate = dateRange.startDate ? dateRange.startDate.toISOString() : null;
        this.pageRequest.endDate = dateRange.endDate ? dateRange.endDate.toISOString() : null;
        this.pageRequest.pageNumber = 1; // Reset to first page when date filter changes
        this.loadStates();
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

    this.canView = this.authorizationService.hasPermission('states', PermissionType.View);
    this.canAddEdit = this.authorizationService.hasPermission('states', PermissionType.AddEdit);
    this.canDelete = this.authorizationService.hasPermission('states', PermissionType.Delete);

    // If view permission was lost, redirect immediately
    if (wasViewable && !this.canView && !this.authorizationService.isAdmin()) {
      this.authorizationService.checkAndRedirectOnPermissionLoss('states', PermissionType.View);
    }

    console.log('States permissions updated:', { canView: this.canView, canAddEdit: this.canAddEdit, canDelete: this.canDelete });
  }

  private initForm(): void {
    this.stateForm = this.fb.group({
      name: ['', [Validators.required, Validators.maxLength(100), CustomValidatorsService.noWhitespaceValidator(), CustomValidatorsService.lettersOnly()]],
      code: ['', [Validators.required, Validators.maxLength(10), CustomValidatorsService.noWhitespaceValidator(), CustomValidatorsService.lettersOnly()]],
      countryId: ['', [Validators.required]],
      isActive: [true]
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

  loadStates(): void {
    this.loading = true;
    const countryId = this.selectedCountryId === 'all' ? undefined : this.selectedCountryId;

    this.stateService.getPagedStates(this.pageRequest, countryId, false).subscribe({
      next: (response) => {
        this.states = response.items;
        this.pagedResponse = response;
        this.loading = false;
      },
      error: (error) => {
        console.error('Error loading states', error);
        const errorMessage = error.error?.message ||
          error.error?.title ||
          error.message ||
          'Failed to load states';
        this.messageService.showMessage({ type: 'error', text: errorMessage });
        this.loading = false;
      }
    });
  }

  onSubmit(): void {
    if (this.stateForm.invalid) {
      return;
    }

    const stateData: State = {
      ...this.stateForm.value,
      id: this.isEditMode && this.currentStateId ? this.currentStateId : undefined,
      createdBy: this.isEditMode ? undefined : this.getUserIdentifier(),
      modifiedBy: this.getUserIdentifier(),
      //isActive: true,
      isDeleted: false
    };

    this.loading = true;
    
    if (this.isEditMode && this.currentStateId) {
      this.stateService.updateState(this.currentStateId, stateData).subscribe({
        next: () => {
          this.messageService.showMessage({ type: 'success', text: 'State updated successfully' });
          this.loadStates();
          this.resetForm();
          this.loading = false;
        },
        error: (error) => {
          console.error('Error updating state:', error);
          const errorMessage = error.error?.message ||
            error.error?.title ||
            error.message ||
            'Failed to update State';
          this.messageService.showMessage({ type: 'error', text: errorMessage });
          this.loading = false;
        }
      });
    } else {
      this.stateService.createState(stateData).subscribe({
        next: () => {
          this.messageService.showMessage({ type: 'success', text: 'State created successfully' });
          this.loadStates();
          this.resetForm();
          this.loading = false;
        },
        error: (error) => {
          console.error('Error creating state:', error);
          const errorMessage = error.error?.message ||
            error.error?.title ||
            error.message ||
            'Failed to create State';
          this.messageService.showMessage({ type: 'error', text: errorMessage });
          this.loading = false;
        }
      });
    }
  }

  editState(state: State): void {
    this.isEditMode = true;
    this.currentStateId = state.id || null;
    this.stateForm.patchValue({
      name: state.name,
      code: state.code,
      countryId: state.countryId,
      modifiedOn: new Date(),
      modifiedBy: this.getUserIdentifier(),
      isActive: state.isActive !== undefined ? state.isActive : true,
      isDeleted: false
    });

    // Use the message service's scrollToTop method
    this.messageService.scrollToTop();
  }

  deleteState(id: string): void {
    if (confirm('Are you sure you want to delete this state?')) {
      this.loading = true;
      this.stateService.deleteState(id).subscribe({
        next: () => {
          this.messageService.showMessage({ type: 'success', text: 'State deleted successfully' });
          this.loadStates();
          this.loading = false;
        },
        error: (error) => {
          console.error('Error deleting state:', error);
          const errorMessage = error.error?.message ||
            error.error?.title ||
            error.message ||
            'Failed to delete State';
          this.messageService.showMessage({ type: 'error', text: errorMessage });
          this.loading = false;
        }
      });
    }
  }

  resetForm(): void {
    this.stateForm.reset({ isActive: true });
    this.isEditMode = false;
    this.currentStateId = null;
  }

  toggleStatus(state: State): void {
    if (!state.id || !this.authorizationService.hasPermission('states', PermissionType.AddEdit)) return;

    this.loading = true;

    // Create a simple update object with just the toggled status
    const update = {
      name: state.name,
      code: state.code,
      countryId: state.countryId,
      isActive: !state.isActive
    };

    this.stateService.updateState(state.id, update).subscribe({
      next: () => {
        // Update the item in the local array to avoid a full reload
        state.isActive = !state.isActive;
        this.messageService.showMessage({
          type: 'success',
          text: `State ${state.isActive ? 'activated' : 'deactivated'} successfully`
        });
        this.loadStates();
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

  getCountryName(countryId: string): string {
    const country = this.countries.find(c => c.id === countryId);
    return country ? country.name : 'Unknown';
  }

  onCountryFilterChange(event: Event): void {
    const select = event.target as HTMLSelectElement;
    this.filterStatesByCountry(select.value);
  }

  filterStatesByCountry(countryId: string): void {
    this.selectedCountryId = countryId;
    this.pageRequest.pageNumber = 1; // Reset to first page when changing filter
    this.loadStates(); // Use the main loadStates method which supports pagination and search
  }

  // Helper method to get user identifier for creation/modification tracking
  private getUserIdentifier(): string {
    return this.currentUser ? this.currentUser.id || this.currentUser.email : 'system';
  }

  // Pagination methods
  onPageChange(page: number): void {
    this.pageRequest.pageNumber = page;
    this.loadStates();
  }

  onPageSizeChange(event: Event): void {
    const selectElement = event.target as HTMLSelectElement;
    this.pageRequest.pageSize = +selectElement.value;
    this.pageRequest.pageNumber = 1;
    this.loadStates();
  }

  onSortChange(column: string): void {
    if (this.pageRequest.sortColumn === column) {
      // Toggle direction if same column is clicked
      this.pageRequest.sortDirection = this.pageRequest.sortDirection === 'asc' ? 'desc' : 'asc';
    } else {
      // Set new column and default to ascending
      this.pageRequest.sortColumn = column;
      this.pageRequest.sortDirection = 'asc';
    }
    this.loadStates();
  }

  onSearch(event: Event): void {
    const searchTerm = (event.target as HTMLInputElement).value;
    this.listService.search(searchTerm);
  }
}
