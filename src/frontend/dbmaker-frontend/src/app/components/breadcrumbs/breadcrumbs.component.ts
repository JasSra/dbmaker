import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, ActivatedRoute, NavigationEnd, RouterModule } from '@angular/router';
import { filter, map } from 'rxjs/operators';

interface Crumb {
  label: string;
  url: string;
}

@Component({
  selector: 'app-breadcrumbs',
  standalone: true,
  imports: [CommonModule, RouterModule],
  template: `
    <nav class="breadcrumbs" aria-label="Breadcrumb" *ngIf="breadcrumbs.length">
      <ol>
        <li *ngFor="let crumb of breadcrumbs; let last = last">
          <a *ngIf="!last" [routerLink]="crumb.url">{{ crumb.label }}</a>
          <span *ngIf="last" aria-current="page">{{ crumb.label }}</span>
        </li>
      </ol>
    </nav>
  `,
  styles: [`
    .breadcrumbs { padding: 12px 24px; }
    .breadcrumbs ol { list-style: none; display: flex; gap: 8px; margin: 0; padding: 0; }
    .breadcrumbs li { color: var(--text-secondary); }
    .breadcrumbs a { color: var(--accent-color); text-decoration: none; }
    .breadcrumbs a:focus-visible { outline: 3px solid var(--accent-color); outline-offset: 2px; border-radius: 4px; }
  `]
})
export class BreadcrumbsComponent {
  breadcrumbs: Crumb[] = [];

  constructor(private router: Router, private route: ActivatedRoute) {
    this.router.events
      .pipe(filter((e): e is NavigationEnd => e instanceof NavigationEnd))
      .subscribe(() => {
        this.breadcrumbs = this.buildBreadCrumbs(this.route.root);
      });
    // initialize
    this.breadcrumbs = this.buildBreadCrumbs(this.route.root);
  }

  private buildBreadCrumbs(route: ActivatedRoute, url: string = '', crumbs: Crumb[] = []): Crumb[] {
    const children = route.children;
    if (!children || children.length === 0) return crumbs;
    for (const child of children) {
      const routeURL = child.snapshot.url.map(segment => segment.path).join('/');
      if (routeURL) url += `/${routeURL}`;
      const label = child.snapshot.data?.['breadcrumb'] || child.snapshot.data?.['title'];
      if (label) crumbs.push({ label, url });
      return this.buildBreadCrumbs(child, url, crumbs);
    }
    return crumbs;
  }
}
