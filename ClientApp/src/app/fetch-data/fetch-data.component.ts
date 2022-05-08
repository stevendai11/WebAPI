import { Component, Inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { catchError, tap, observable, pipe } from 'rxjs';

@Component({
  selector: 'app-fetch-data',
  styleUrls: ['./fetch-data.component.scss'],
  templateUrl: './fetch-data.component.html'
})
export class FetchDataComponent {
  public forecast: WeatherForecast;
  public city: string='Vancouver';
  public temperature:number;
  baseUrl:any;
  public errorMsg:string = '';
  constructor(public http: HttpClient, @Inject('BASE_URL') baseUrl) {
    this.baseUrl = baseUrl;    
  }
  public onSubmit()
  {
    this.errorMsg = '';
    this.forecast=null;
    const url = `${this.baseUrl}WeatherForecastController/?city=${this.city}`;
    this.http.get<WeatherForecast>(url)
    .pipe(
      tap(result => {
        this.forecast = result;
        this.temperature = result.temperature;}), 
      catchError((err) => {
        this.errorMsg = JSON.stringify(err);
        return this.errorMsg;
      })).subscribe(_ => console.log("water is flowing!"));;   
    }
  }

interface WeatherForecast {
  lastdatetime: string;
  temperature: number;
  city: string;
}
