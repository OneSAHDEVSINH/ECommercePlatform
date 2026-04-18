import { Component, OnInit, OnDestroy, ChangeDetectorRef } from '@angular/core';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { Country } from '../../models/country.model';
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
  selector: 'app-country',
  templateUrl: './country.component.html',
  styleUrls: ['./country.component.scss'],
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule, PaginationComponent, DateRangeFilterComponent]
})

export class CountryComponent implements OnInit, OnDestroy {
  countries: Country[] = [];
  countryForm!: FormGroup;
  isEditMode: boolean = false;
  currentCountryId: string | null = null;
  loading: boolean = false;
  message: Message | null = null;
  private currentUser: any = null;
  private messageSubscription!: Subscription;
  private searchSubscription!: Subscription;
  private dateRangeSubscription!: Subscription;
  private permissionSubscription!: Subscription;
  Math = Math;
  PermissionType = PermissionType;

  // Permission-dependent UI state
  canAddEdit: boolean = false;
  canDelete: boolean = false;
  canView: boolean = false;

  // Filter properties
  selectedStatusFilter: string = 'all';

  // Pagination properties
  pagedResponse: PagedResponse<Country> | null = null;
  pageRequest: PagedRequest = {
    pageNumber: 1,
    pageSize: 10,
    searchText: '',
    sortColumn: 'name',
    sortDirection: 'asc'
  };

  constructor(
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
      this.loadCountries();
    });

    // Subscribe to date range changes
    this.dateRangeSubscription = this.dateFilterService.getDateRangeObservable()
      .subscribe((dateRange: DateRange) => {
        this.pageRequest.startDate = dateRange.startDate ? dateRange.startDate.toISOString() : null;
        this.pageRequest.endDate = dateRange.endDate ? dateRange.endDate.toISOString() : null;
        this.pageRequest.pageNumber = 1;
        this.loadCountries();
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

    this.canView = this.authorizationService.hasPermission('countries', PermissionType.View);
    this.canAddEdit = this.authorizationService.hasPermission('countries', PermissionType.AddEdit);
    this.canDelete = this.authorizationService.hasPermission('countries', PermissionType.Delete);

    // If view permission was lost, redirect immediately
    if (wasViewable && !this.canView && !this.authorizationService.isAdmin()) {
      this.authorizationService.checkAndRedirectOnPermissionLoss('countries', PermissionType.View);
    }

    console.log('Countries permissions updated:', { canView: this.canView, canAddEdit: this.canAddEdit, canDelete: this.canDelete });
  }

  private initForm(): void {
    this.countryForm = this.fb.group({
      name: ['', [Validators.required, Validators.maxLength(100), CustomValidatorsService.noWhitespaceValidator(), CustomValidatorsService.lettersOnly()]],
      code: ['', [Validators.required, Validators.maxLength(10), CustomValidatorsService.noWhitespaceValidator(), CustomValidatorsService.lettersOnly()]],
      isActive: [true]
    });
  }

  loadCountries(): void {
    this.loading = true;
    const isActive = this.selectedStatusFilter === 'all' ? undefined :
      this.selectedStatusFilter === 'active' ? true : false;
    this.countryService.getPagedCountries(this.pageRequest, false).subscribe({
      next: (response) => {
        this.pagedResponse = response;
        this.countries = response.items;
        this.loading = false;
      },
      error: (error) => {
        console.error('Error loading countries:', error);
        if (error.error) {
          console.error('Server validation errors:', error.error);
        }
        const errorMessage = error.error?.message ||
          error.error?.title ||
          error.message ||
          'Failed to load countries';
        this.messageService.showMessage({ type: 'error', text: errorMessage });
        this.loading = false;
      }
    });
  }

  onSubmit(): void {

    if (this.countryForm.invalid) {
      return;
    }

    const countryData: Country = {
      ...this.countryForm.value,
      id: this.isEditMode && this.currentCountryId ? this.currentCountryId : undefined,
      //name: this.countryForm.value.name?.trim(),
      //code: this.countryForm.value.code?.trim(),
      createdOn: new Date(),
      createdBy: this.isEditMode ? undefined : this.getUserIdentifier(),
      //createdBy: "System",
      modifiedOn: new Date(),
      modifiedBy: this.getUserIdentifier(),
      //modifiedBy: "System",
      //isActive: true,
      isDeleted: false
    };

    this.loading = true;

    if (this.isEditMode && this.currentCountryId) {
      this.countryService.updateCountry(this.currentCountryId, countryData).subscribe({
        next: () => {
          this.messageService.showMessage({ type: 'success', text: 'Country updated successfully' });
          this.loadCountries();
          this.resetForm();
          this.loading = false;
        },
        error: (error) => {
          console.error('Error updating country:', error);
          // And in the updateCountry method's error handler
          if (error.error) {
            console.error('Server validation errors:', error.error);
            console.error('Request payload:', countryData);
          }
          // Extract the most useful error message
          const errorMessage = error.error?.message ||
            error.error?.title ||
            error.message ||
            'Failed to update country';
          this.messageService.showMessage({ type: 'error', text: errorMessage });
          this.loading = false;
        }
      });
    } else {
      this.countryService.createCountry(countryData).subscribe({
        next: () => {
          this.messageService.showMessage({ type: 'success', text: 'Country created successfully' });
          this.loadCountries();
          this.resetForm();
          this.loading = false;
        },
        error: (error) => {
          console.error('Error creating country:', error);
          // Log more details about the error response
          if (error.error) {
            console.error('Server validation errors:', error.error);
          }
          // Extract the most useful error message
          const errorMessage = error.error?.message ||
            error.error?.title ||
            error.message ||
            'Failed to create country';
          this.messageService.showMessage({ type: 'error', text: errorMessage });
          this.loading = false;
        }
      });
    }
  }

  editCountry(country: Country): void {
    this.isEditMode = true;
    this.currentCountryId = country.id || null;
    this.countryForm.patchValue({
      name: country.name,
      code: country.code,
      //createdOn: new Date(),
      //createdBy: this.isEditMode ? undefined : this.getUserIdentifier(),
      modifiedOn: new Date(),
      modifiedBy: this.getUserIdentifier(),
      isActive: country.isActive,
      isDeleted: false
    });

    // Use the message service's scrollToTop method
    this.messageService.scrollToTop();
  }

  deleteCountry(id: string): void {
    if (confirm('Are you sure you want to delete this country?')) {
      this.loading = true;
      this.countryService.deleteCountry(id).subscribe({
        next: () => {
          this.messageService.showMessage({ type: 'success', text: 'Country deleted successfully' });
          this.loadCountries();
          this.loading = false;
        },
        error: (error) => {
          console.error('Error deleting country:', error);
          // Log more details about the error response
          if (error.error) {
            console.error('Server validation errors:', error.error);
          }
          // Extract the most useful error message
          const errorMessage = error.error?.message ||
            error.error?.title ||
            error.message ||
            'Failed to delete country';
          this.messageService.showMessage({ type: 'error', text: errorMessage });
          this.loading = false;
        }
      });
    }
  }

  resetForm(): void {
    this.countryForm.reset({ isActive: true });
    this.isEditMode = false;
    this.currentCountryId = null;
  }

  toggleStatus(country: Country): void {
    if (!country.id || !this.authorizationService.hasPermission('countries', PermissionType.AddEdit)) return;

    this.loading = true;

    // Create a simple update object with just the toggled status
    const update = {
      name: country.name,
      code: country.code,
      isActive: !country.isActive
    };

    this.countryService.updateCountry(country.id, update).subscribe({
      next: () => {
        // Update the item in the local array to avoid a full reload
        country.isActive = !country.isActive;
        this.messageService.showMessage({
          type: 'success',
          text: `Country ${country.isActive ? 'activated' : 'deactivated'} successfully`
        });
        this.loadCountries();
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

  // Helper method to get user identifier for creation/modification tracking
  private getUserIdentifier(): string {
    return this.currentUser ? this.currentUser.id || this.currentUser.email : 'system';
  }

  // Pagination methods
  onPageChange(page: number): void {
    this.pageRequest.pageNumber = page;
    this.loadCountries();
  }

  onPageSizeChange(event: Event): void {
    const selectElement = event.target as HTMLSelectElement;
    this.pageRequest.pageSize = +selectElement.value; 
    this.pageRequest.pageNumber = 1;
    this.loadCountries();
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
    this.loadCountries();
  }

  // Update onSearch method to properly type the event
  onSearch(event: Event): void {
    const searchTerm = (event.target as HTMLInputElement).value;
    this.listService.search(searchTerm);
  }
}
