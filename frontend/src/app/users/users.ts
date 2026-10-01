import { Component, signal, inject, OnInit } from '@angular/core';
import { MessageService } from 'primeng/api';
import { HttpClient } from '@angular/common/http';
import { User } from '../_models/user';
import { FormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { TableModule } from 'primeng/table';
import { ToastModule } from 'primeng/toast';
import { environment } from '../../environments/environment';

@Component({
  imports: [ButtonModule, TableModule, DialogModule, ToastModule, FormsModule],
  providers: [MessageService],
  selector: 'app-users',
  styleUrl: './users.less',
  templateUrl: './users.html',
})
export class Users implements OnInit {
  displayDialog: boolean = false;
  user: User = new User();
  users = signal<User[]>([]);
  api = inject(HttpClient);
  baseUrl = environment.apiUrl;
  private messageService = inject(MessageService);

  addUser() {
    this.user = new User();
    this.displayDialog = true;
  }

  saveUser() {
    if (this.user.firstName && this.user.lastName && this.user.email) {
      this.api.post(this.baseUrl + '/account/register', this.user).subscribe({
        next: (user: User) => {
          this.users.update(users => [...users, user]);
          this.displayDialog = false;
          this.messageService.add({severity:'success', summary: 'Success', detail: 'User saved successfully'});
        },
        error: error => {
          this.messageService.add({severity:'error', summary: 'Error', detail: 'Failed to save user'});
        }
      });
    }
  }

  isLoading() {
    return false;
  }

  ngOnInit(): void {
    this.api.get<User[]>(this.baseUrl + '/account/users').subscribe({
      next: (users: User[]) => {
        this.users.set(users);
      },
      error: error => {
        this.messageService.add({severity:'error', summary: 'Error', detail: 'Failed to load users'});
      }
    });
  }
}
