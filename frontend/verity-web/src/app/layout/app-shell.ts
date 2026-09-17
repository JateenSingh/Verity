import { Component, inject } from '@angular/core';
import { Router, RouterLink, RouterOutlet } from '@angular/router';
import { AuthStore } from '../core/auth/auth-store';

@Component({
  selector: 'app-shell',
  imports: [RouterLink, RouterOutlet],
  templateUrl: './app-shell.html',
  styleUrl: './app-shell.scss',
})
export class AppShell {
  protected readonly authStore = inject(AuthStore);
  private readonly router = inject(Router);

  protected logout(): void {
    this.authStore.logout();
    void this.router.navigate(['/posts']);
  }
}
