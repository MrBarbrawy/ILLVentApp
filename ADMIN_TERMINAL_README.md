# ILLVent Admin Terminal

## Overview

The ILLVent Admin Terminal is a simple and efficient MVC-based administration dashboard that provides comprehensive management capabilities for your healthcare application. It follows the same authentication pattern as the hospital terminal - simple session-based authentication without JWT complications.

## Features

### 🔐 Authentication
- Simple login interface similar to hospital terminal
- Session-based authentication (no JWT complexity)
- Role-based access control (Admin role required)

### 📊 Dashboard
- System overview with key statistics
- Quick action buttons for common tasks
- Real-time data display

### 🛠️ Management Capabilities

#### 1. Product Management
- ✅ View all products
- ✅ Add new products
- ✅ Update existing products
- ✅ Delete products
- ✅ Manage inventory and pricing

#### 2. User & Role Management
- ✅ View all users
- ✅ Assign roles to users (User, Admin, Doctor, Hospital, Driver)
- ✅ Remove roles from users
- ✅ View user verification status

#### 3. Hospital Management
- ✅ View all registered hospitals
- ✅ Add new hospitals
- ✅ Delete hospitals
- ✅ Manage hospital information

#### 4. Pharmacy Management
- ✅ View all registered pharmacies
- ✅ Add new pharmacies
- ✅ Delete pharmacies
- ✅ Manage pharmacy information

#### 5. System Logs
- ✅ View system activity logs
- ✅ Filter logs by level (Info, Warning, Error)
- ✅ Monitor system health

## Getting Started

### Default Admin Credentials

When you run the application in development mode, a default admin user is automatically created:

```
Email: admin@illventapp.com
Password: Admin123!
```

**⚠️ Important:** Change these credentials in production!

### Accessing the Admin Terminal

1. Start your application
2. Navigate to: `https://localhost:5001/AdminView/Login`
3. Login with the admin credentials
4. You'll be redirected to the admin dashboard

### URL Routes

- **Login**: `/AdminView/Login`
- **Dashboard**: `/AdminView/Dashboard`
- **Products**: `/AdminView/Products`
- **Users**: `/AdminView/Users`
- **Hospitals**: `/AdminView/Hospitals`
- **Pharmacies**: `/AdminView/Pharmacies`
- **Logs**: `/AdminView/Logs`

## API Endpoints

The admin terminal uses RESTful API endpoints for data operations:

### Products
- `GET /api/admin/products` - Get all products
- `POST /api/admin/products` - Create product
- `PUT /api/admin/products/{id}` - Update product
- `DELETE /api/admin/products/{id}` - Delete product

### Users & Roles
- `GET /api/admin/users` - Get all users
- `GET /api/admin/roles` - Get all roles
- `POST /api/admin/users/{userId}/roles` - Assign role
- `DELETE /api/admin/users/{userId}/roles/{roleName}` - Remove role

### Hospitals
- `GET /api/admin/hospitals` - Get all hospitals
- `POST /api/admin/hospitals` - Create hospital
- `DELETE /api/admin/hospitals/{id}` - Delete hospital

### Pharmacies
- `GET /api/admin/pharmacies` - Get all pharmacies
- `POST /api/admin/pharmacies` - Create pharmacy
- `DELETE /api/admin/pharmacies/{id}` - Delete pharmacy

### System Logs
- `GET /api/admin/logs` - Get system logs

## Architecture

### Controllers
- **AdminViewController**: Handles view rendering and authentication
- **AdminApiController**: Provides RESTful API endpoints for data operations

### Views
- **Login.cshtml**: Admin login interface
- **Dashboard.cshtml**: Main admin dashboard with all functionality

### Security
- All admin routes require `[Authorize(Roles = "Admin")]`
- Session-based authentication
- CSRF protection on forms
- Input validation and sanitization

## Styling & UI

The admin terminal features:
- Modern, responsive Bootstrap 5 design
- Dark admin theme with professional colors
- Font Awesome icons
- Hover effects and smooth transitions
- Mobile-friendly responsive layout

## Logging

The system automatically logs all admin actions:
- User login/logout events
- CRUD operations (Create, Read, Update, Delete)
- Role assignments
- System errors and warnings

## Development Notes

### Adding New Features

1. **New Management Section**: Add new nav item in Dashboard.cshtml
2. **New API Endpoint**: Add to AdminApiController.cs
3. **New View**: Create corresponding view section in Dashboard.cshtml

### Customization

- **Colors**: Modify CSS variables in Dashboard.cshtml
- **Logo**: Update navbar brand section
- **Features**: Add new sections following existing patterns

## Security Considerations

1. **Production Setup**:
   - Change default admin password
   - Use environment variables for sensitive data
   - Enable HTTPS
   - Configure proper CORS policies

2. **Role Management**:
   - Regularly audit user roles
   - Follow principle of least privilege
   - Monitor admin activities

3. **Data Protection**:
   - Validate all inputs
   - Sanitize user data
   - Use parameterized queries

## Troubleshooting

### Common Issues

1. **Can't access admin terminal**:
   - Ensure you're using admin credentials
   - Check if Admin role exists in database
   - Verify user has Admin role assigned

2. **API endpoints not working**:
   - Check authentication status
   - Verify admin role assignment
   - Check browser console for errors

3. **Data not loading**:
   - Check database connection
   - Verify service registrations
   - Check application logs

### Support

For technical issues or feature requests, please check the application logs and ensure all dependencies are properly configured.

## Future Enhancements

Potential improvements for the admin terminal:
- Advanced filtering and search
- Bulk operations
- Data export functionality
- Real-time notifications
- Advanced analytics dashboard
- Audit trail with detailed history 