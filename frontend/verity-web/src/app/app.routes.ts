import { Routes } from '@angular/router';
import { authGuard } from './core/auth/auth.guard';
import { guestGuard } from './core/auth/guest.guard';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'posts' },
  {
    path: 'posts',
    loadComponent: () => import('./features/posts/post-list/post-list.page').then((m) => m.PostListPage),
  },
  {
    path: 'posts/new',
    loadComponent: () => import('./features/posts/post-create/post-create.page').then((m) => m.PostCreatePage),
    canActivate: [authGuard],
  },
  {
    path: 'posts/:id',
    loadComponent: () => import('./features/posts/post-detail/post-detail.page').then((m) => m.PostDetailPage),
  },
  {
    path: 'login',
    loadComponent: () => import('./features/auth/login/login.page').then((m) => m.LoginPage),
    canActivate: [guestGuard],
  },
  {
    path: 'register',
    loadComponent: () => import('./features/auth/register/register.page').then((m) => m.RegisterPage),
    canActivate: [guestGuard],
  },
  {
    path: '**',
    loadComponent: () => import('./shared/ui/not-found.page').then((m) => m.NotFoundPage),
  },
];
