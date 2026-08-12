import { Component, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';

import { Sidebar } from './shared/components/sidebar/sidebar';
import { Toast } from './shared/components/toast/toast';
import { SidebarStateService } from './shared/services/sidebar-state.service';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, Sidebar, Toast],
  templateUrl: './app.html',
  styleUrl: './app.scss'
})
export class App {
  private readonly sidebarState = inject(SidebarStateService);

  readonly collapsed = this.sidebarState.collapsed;
}
