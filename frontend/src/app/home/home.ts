import { Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { AccountService } from '../_services/account.service';

@Component({
  imports: [RouterLink, ButtonModule],
  selector: 'app-home',
  styleUrls: ['./home.less'],
  templateUrl: './home.html',
})
export class Home {
  protected account = inject(AccountService);

  protected readonly steps = [
    { title: 'Create a project', text: 'Give it a name. Its short key (e.g. WEB) becomes the prefix of every task.' },
    { title: 'Add tasks fast', text: 'Type a title and press Enter. Add details later, only when you need them.' },
    { title: 'Track progress', text: 'Move tasks across the board, or switch to the list view to scan everything.' },
  ];
}
