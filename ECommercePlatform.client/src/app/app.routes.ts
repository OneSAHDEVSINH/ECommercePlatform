import { Routes, UrlSegment } from '@angular/router';
import { LoginComponent } from './admin/login/login.component';
import { DashboardComponent } from './admin/dashboard/dashboard.component';
import { CountryComponent } from './admin/country/country.component';
import { StateComponent } from './admin/state/state.component';
import { CityComponent } from './admin/city/city.component';
import { authGuard } from './guards/auth.guard';
import { PageNotFoundComponent } from './page-not-found/page-not-found.component';
import { AdminLayoutComponent } from './shared/admin-layout/admin-layout.component';


// Custom matcher to catch malformed URLs
function malformedUrlMatcher(url: UrlSegment[]) {
  const fullUrl = url.map(segment => segment.path).join('/');

  // Check if URL contains malformed encoding
  if (fullUrl.includes('%F') ||
    fullUrl.includes('%f') ||
    fullUrl.match(/%[0-9A-F]([^0-9A-F]|$)/i)) {
    return {
      consumed: url,
      posParams: {
        malformedUrl: new UrlSegment(fullUrl, {})
      }
    };
  }

  return null;
}

export const routes: Routes = [

  {
    matcher: malformedUrlMatcher,
    component: PageNotFoundComponent
  },
  {
    path: '',
    redirectTo: 'admin/login',
    pathMatch: 'full'
  },
  {
    path: 'admin',
    children: [
      {
        path: 'login',
        component: LoginComponent,
        title: 'Admin Login'
      },
      {
        path: '',
        component: AdminLayoutComponent,
        canActivate: [authGuard],
        children: [
          {
            path: 'dashboard',
            component: DashboardComponent,
            title: 'Admin Dashboard'
          },
          {
            path: 'countries',
            component: CountryComponent,
            title: 'Country Management'
          },
          {
            path: 'states',
            component: StateComponent,
            title: 'State Management'
          },
          {
            path: 'cities',
            component: CityComponent,
            title: 'City Management'
          },
          {
            path: '',
            redirectTo: 'dashboard',
            pathMatch: 'full'
          }
        ]
      }
    ]
  },
  {
    path: '**',
    component: PageNotFoundComponent,
    title: '404 - Page not found'
  }
];


//export const routes: Routes = [
//  {
//    path: '',
//    redirectTo: 'admin/login',
//    pathMatch: 'full'
//  },
//  {
//    path: 'admin/login',
//    component: LoginComponent,
//    title: 'Admin Login'
//  },
//  {
//    path: 'admin/dashboard',
//    component: DashboardComponent,
//    title: 'Admin Dashboard',
//    canActivate: [authGuard]
//  },
//  {
//    path: 'admin/countries',
//    component: CountryComponent,
//    title: 'Country Component',
//    canActivate: [authGuard]
//  },
//  {
//    path: 'admin/states',
//    component: StateComponent,
//    title: 'State Component',
//    canActivate: [authGuard]
//  },
//  {
//    path: 'admin/cities',
//    component: CityComponent,
//    title: 'City Component',
//    canActivate: [authGuard]
//  },
//  {
//    path: '**',
//    component: PageNotFoundComponent,
//    pathMatch: 'full',
//    title: '404 - Page not found'
//  }
//];
