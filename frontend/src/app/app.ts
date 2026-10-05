import { Component, ElementRef, inject, signal, viewChild } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { Header } from './header/header';
import { Menu } from './menu/menu';
import { LayoutService } from './_services/layout.service';

@Component({
  imports: [RouterOutlet, Header, Menu],
  selector: 'app-root',
  styleUrl: './app.less',
  templateUrl: './app.html',
})
export class App {
  protected layout = inject(LayoutService);
  protected readonly title = signal('pjm');
  private readonly main = viewChild.required<ElementRef<HTMLElement>>('main');

  // A plain "#main-content" href would navigate to the base href, so move focus manually.
  skipToContent(event: Event) {
    event.preventDefault();
    this.main().nativeElement.focus();
  }
}
