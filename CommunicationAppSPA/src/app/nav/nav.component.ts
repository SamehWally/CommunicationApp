import { AuthService } from './../_services/auth.service';
import { UserService } from '../_services/user.service';

import { Component, OnInit } from '@angular/core';
import { HubConnection, HubConnectionBuilder } from '@aspnet/signalr';
import { Router } from '@angular/router';

@Component({
  selector: 'app-nav',
  templateUrl: './nav.component.html',
  styleUrls: ['./nav.component.css']
})
export class NavComponent implements OnInit {

constructor() {

}
ngOnInit(): void {

}
}
