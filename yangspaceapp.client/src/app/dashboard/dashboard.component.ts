import { Component, OnInit } from '@angular/core';

@Component({
  selector: 'app-dashboard',
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.css']
})
export class DashboardComponent implements OnInit {
  tasks = [];

  constructor(private taskService: TaskService) {}

  ngOnInit(): void {
    this.taskService.getTasks().subscribe(tasks => {
      this.tasks = tasks;
    });
  }

  markCompleted(task) {
    task.status = 'Completed';
    this.taskService.updateTask(task); // Update task status
  }

  cancelTask(task) {
    task.status = 'Canceled';
    this.taskService.updateTask(task); // Update task status
  }
}
