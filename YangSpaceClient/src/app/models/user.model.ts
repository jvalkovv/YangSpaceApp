export interface User {
  id?: string;
  username: string;
  email: string;
  firstName: string;
  lastName: string;
  role: 'OrdinaryUser' | 'ServiceProvider' | 'Admin';
  isServiceProvider: boolean;
}

export interface UserRegistrationModel extends Omit<User, 'id' | 'role'> {
  password: string;
  confirmPassword: string;
}

export interface UserLoginModel {
  username: string;
  password: string;
}

export interface UserProfileUpdateModel {
  firstName: string;
  lastName: string;
  email: string;
}
