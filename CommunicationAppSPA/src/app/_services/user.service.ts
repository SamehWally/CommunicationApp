import { Injectable } from '@angular/core';
import { environment } from 'src/environments/environment';
import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { User } from '../_models/user';
import { PaginationResult } from '../_models/Pagination';
import { map, tap } from 'rxjs/operators';
import { Message } from '../_models/message';

@Injectable({
  providedIn: 'root'
})
export class UserService {

baseUrl = environment.apiUrl + 'users/';

constructor(private http: HttpClient) { }

}
