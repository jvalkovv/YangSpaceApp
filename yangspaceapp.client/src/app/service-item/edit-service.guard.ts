import { Injectable } from "@angular/core";
import { ActivatedRouteSnapshot, CanActivate, Router, RouterStateSnapshot } from "@angular/router";
import { Observable } from "rxjs";
import { AuthService } from "../auth/services/auth-service";

@Injectable({
  providedIn: 'root',
})
export class EditServiceGuard implements CanActivate {
  constructor(private authService: AuthService, private router: Router) { }

  canActivate(
    route: ActivatedRouteSnapshot,
    state: RouterStateSnapshot
  ): Observable<boolean> | Promise<boolean> | boolean {
    const serviceId = route.paramMap.get('id');
    if (serviceId) {
      return this.authService.checkServiceAccess(Number(serviceId)).toPromise().then(
        (hasAccess) => {
          // Ensure that the returned value is a boolean
          return hasAccess ?? false;
        },
        () => {
          // Handle error, return false in case of any failure
          this.router.navigate(['/homepage']);
          return false;
        }
      );
    }
    return false;
  }
}
