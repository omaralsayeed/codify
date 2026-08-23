import { Component, inject } from '@angular/core';
import { RouterOutlet, Router, NavigationEnd } from '@angular/router';
import { NavbarComponent } from './shared/components/navbar/navbar.component';
import { FooterComponent } from './shared/components/footer/footer.component';
import { PlanRedirectService } from './core/services/plan-redirect.service';
import { filter, map } from 'rxjs/operators';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, NavbarComponent, FooterComponent],
  templateUrl: './app.html',
})
export class App {
  hideLayout = false;

  // Injecting here ensures the service is instantiated on app boot,
  // so it reads ?plan= before anything else renders.
  private readonly planRedirect = inject(PlanRedirectService);

  constructor(private router: Router) {
    this.router.events
      .pipe(
        filter(event => event instanceof NavigationEnd),
        map(() => {
          let route = this.router.routerState.root;
          while (route.firstChild) {
            route = route.firstChild;
          }
          return route.snapshot.data['hideLayout'] || false;
        })
      )
      .subscribe(hideLayout => {
        this.hideLayout = hideLayout;
      });
  }
}
