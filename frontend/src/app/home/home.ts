import { Component, inject } from '@angular/core';
import { environment } from '../../environments/environment';
import { AuthLogin } from '../_models/login';
import { MessageService } from 'primeng/api';
import { OnInit } from '@angular/core';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { FormsModule } from '@angular/forms';
import { Login } from '../_models/login';
import { HttpClient } from '@angular/common/http';
import { ToastModule } from 'primeng/toast';

@Component({
  imports: [ButtonModule, DialogModule, ToastModule, FormsModule],
  providers: [MessageService],
  selector: 'app-home',
  styleUrls: ['./home.less'],
  templateUrl: './home.html',
})
export class Home implements OnInit {
  displayDialog: boolean = false;
  loginForm: Login = new Login();
  apiUrl = environment.apiUrl;
  api = inject(HttpClient);
  messageToast = inject(MessageService);

  ngOnInit(): void {
  }

  onLogin() {
    this.api.post(this.apiUrl + '/account/login', this.loginForm).subscribe({
      next: (response) => {
      console.log('Login successful', response);
      sessionStorage.setItem('pjmToken', (response as AuthLogin).token);
      this.messageToast.add({severity:'success', summary: 'Login Successful', detail: 'You have successfully logged in.'});
      },
      error: error => {
        console.error('Login failed', error);
        this.messageToast.add({severity:'error', summary: 'Login Failed', detail: 'Invalid username or password.'});
      }
    });
  }

}
