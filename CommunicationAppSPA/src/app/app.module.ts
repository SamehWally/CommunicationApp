import { NgModule } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';

import { AppComponent } from './app.component';
import { NavComponent } from './nav/nav.component';
import { AuthService } from './_services/auth.service';
import { UserService } from './_services/user.service';
import { HomeComponent } from './home/home.component';

@NgModule({
  declarations: [	
    AppComponent,
      NavComponent,
      HomeComponent
   ],
  imports: [
    BrowserModule
  ],
  providers: [
    AuthService,
    UserService
  ],
  bootstrap: [AppComponent]
})
export class AppModule { }
