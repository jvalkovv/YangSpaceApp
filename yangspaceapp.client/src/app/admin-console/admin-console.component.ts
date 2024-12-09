import { Component, OnInit } from '@angular/core';

@Component({
  selector: 'app-admin-console',
  templateUrl: './admin-console.component.html',
  styleUrls: ['./admin-console.component.css']
})
export class AdminConsoleComponent implements OnInit {
  users = [];

  constructor(private adminService: AdminService) {}

  ngOnInit(): void {
    this.adminService.getUsers().subscribe(users => {
      this.users = users;
    });
  }

  addUser() {
    this.adminService.addUser();
  }

  editUser(user) {
    this.adminService.editUser(user);
  }

  deleteUser(user) {
    this.adminService.deleteUser(user);
  }
}
