import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MsalService } from '@azure/msal-angular';
import { ContainerService } from '../services/container.service';
import { UserService } from '../services/user.service';
import { ContainerResponse, ContainerMonitoringData } from '../models/container.models';

@Component({
  selector: 'app-dashboard',
  imports: [CommonModule],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.scss'
})
export class DashboardComponent implements OnInit {
  containers: ContainerResponse[] = [];
  userStats: any = {};
  monitoring: ContainerMonitoringData[] = [];

  constructor(
    private msalService: MsalService,
    private containerService: ContainerService,
    private userService: UserService
  ) {}

  ngOnInit() {
    this.loadDashboardData();
    this.setupMonitoring();
  }

  private loadDashboardData() {
    this.containerService.getContainers().subscribe(containers => {
      this.containers = containers;
    });

    this.userService.getUserStats().subscribe(stats => {
      this.userStats = stats;
    });
  }

  private setupMonitoring() {
    const eventSource = this.containerService.getMonitoringStream();
    eventSource.onmessage = (event) => {
      try {
        this.monitoring = JSON.parse(event.data);
      } catch (error) {
        console.error('Error parsing monitoring data:', error);
      }
    };
  }

  logout() {
    this.msalService.logout();
  }
}
