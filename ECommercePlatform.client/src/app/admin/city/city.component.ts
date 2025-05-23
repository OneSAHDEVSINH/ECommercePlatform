import { Component, OnInit, OnDestroy } from '@angular/core';
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
import { NavbarComponent } from "../navbar/navbar.component";
import { CustomValidatorsService } from '../../services/custom-validators/custom-validators.service';

@Component({
  selector: 'app-city',
  templateUrl: './city.component.html',
  styleUrls: ['./city.component.scss'],
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule, FormsModule, NavbarComponent]
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
  //stateId: string = '';
  message: Message | null = null;
  private currentUser: any = null;
  private messageSubscription!: Subscription;

  noWhitespaceValidator(): ValidatorFn {
    return (control: AbstractControl): { [key: string]: any } | null => {
      // Check if the value exists and if it contains only whitespace
      const isWhitespace = control.value && control.value.trim().length === 0;
      // Return validation error if true, otherwise null
      return isWhitespace ? { 'whitespace': true } : null;
    };
  }

  constructor(
    private cityService: CityService,
    private stateService: StateService,
    private countryService: CountryService,
    private authService: AuthService,
    private messageService: MessageService,
    private fb: FormBuilder
  ) { }

  ngOnInit(): void {
    this.initForm();
    this.loadCountries();
    this.loadStates();
    this.loadCities();
    this.authService.currentUser$.subscribe(user => {
      this.currentUser = user;
    });
    //this.cityForm.get('stateId')?.disable(); // Disable the state dropdown
    // Subscribe to message changes
    this.messageSubscription = this.messageService.currentMessage.subscribe(message => {
      this.message = message;
    });
  }

  ngOnDestroy(): void {
    // Clean up subscriptions
    if (this.messageSubscription) {
      this.messageSubscription.unsubscribe();
    }
  }

  private initForm(): void {
    this.cityForm = this.fb.group({
      name: ['', [Validators.required, Validators.maxLength(100), CustomValidatorsService.noWhitespaceValidator(), CustomValidatorsService.lettersOnly()]],
      stateId: ['', [Validators.required]]
    });
  }


  loadCountries(): void {
    this.countryService.getCountries().subscribe({
      next: (countries) => {
        this.countries = countries;
      },
      error: (error) => {
        console.error('Error loading countries:', error);
        this.messageService.showMessage({ type: 'error', text: 'Failed to load countries' });
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
          this.messageService.showMessage({ type: 'error', text: 'Failed to load states for the selected country' });
        }
      });
    } else {
      this.stateService.getStates().subscribe({
        next: (states) => {
          this.states = states;
        },
        error: (error) => {
          console.error('Error loading states:', error);
          this.messageService.showMessage({ type: 'error', text: 'Failed to load states' });
        }
      });
    }
  }

  loadCities(): void {
    this.loading = true;
    this.cityService.getCities().subscribe({
      next: (cities) => {
        this.cities = cities;
        this.loading = false;
      },
      error: (error) => {
        console.error('Error loading cities:', error);
        this.messageService.showMessage({ type: 'error', text: 'Failed to load cities' });
        this.loading = false;
      }
    });
  }

  filterCitiesByState(stateId: string): void {
    if (!stateId || stateId === 'all') {
      this.loadCities();
      return;
    }

    this.loading = true;
    this.cityService.getCitiesByState(stateId).subscribe({
      next: (cities) => {
        this.cities = cities;
        this.loading = false;
      },
      error: (error) => {
        console.error('Error loading cities by state:', error);
        this.messageService.showMessage({ type: 'error', text: 'Failed to load cities for the selected state' });
        this.loading = false;
      }
    });
  }

  onStateFilterChange(event: Event): void {
    const select = event.target as HTMLSelectElement;
    this.filterCitiesByState(select.value);
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
      isActive: true,
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

  //editCity(city: City): void {
  //  this.isEditMode = true;
  //  this.currentCityId = city.id || null;

  //  // Find the state to get the associated country
  //  const state = this.states.find(s => s.id === city.stateId);
  //  if (state) {
  //    this.selectedCountryId = state.countryId;
  //    this.loadStates(state.countryId);
  //    //this.stateId = city.stateId;
  //  }

  //  this.cityForm.patchValue({
  //    name: city.name,
  //    stateId: city.stateId,
  //    modifiedOn: new Date(),
  //    modifiedBy: this.getUserIdentifier(),
  //    isActive: true,
  //    isDeleted: false
  //  });
  //}

  editCity(city: City): void {
    this.isEditMode = true;
    this.currentCityId = city.id || null;

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
              stateId: city.stateId
            });
          });
        },
        error: (error) => {
          console.error('Error loading states for city editing:', error);
          this.messageService.showMessage({
            type: 'error',
            text: 'Failed to load states for the selected country'
          });
        }
      });
    } else {
      // If we can't find the state in our current list,
      // we need to load all states first to ensure we have the right data
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
                    stateId: city.stateId
                  });
                });
              }
            });
          } else {
            // If still can't find state, just patch form with what we have
            this.cityForm.patchValue({
              name: city.name,
              stateId: city.stateId
            });
          }
        },
        error: (error) => {
          console.error('Error loading all states for city editing:', error);
          this.messageService.showMessage({
            type: 'error',
            text: 'Failed to load states'
          });
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
          this.messageService.showMessage({ type: 'error', text: 'Failed to delete city' });
          this.loading = false;
        }
      });
    }
  }

  resetForm(): void {
    this.cityForm.reset();
    this.isEditMode = false;
    this.currentCityId = null;
    this.selectedCountryId = '';
  }

  getStateName(stateId: string): string {
    const state = this.states.find(s => s.id === stateId);
    return state ? state.name : 'Unknown';
  }

  getCountryForState(stateId: string): string {
    const state = this.states.find(s => s.id === stateId);
    if (!state) return 'Unknown';
    
    const country = this.countries.find(c => c.id === state.countryId);
    return country ? country.name : 'Unknown';
  }

  // Helper method to get user identifier for creation/modification tracking
  private getUserIdentifier(): string {
    return this.currentUser ? this.currentUser.id || this.currentUser.email : 'system';
  }
}
