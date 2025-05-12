---
---

# Phase B: Frontend for team management

## Phase description

**Purpose:** This phase implements the frontend interface for managing teams, including
- Teams list page with team data display
- Add team modal dialog for team creation
- Team details side pane for viewing, editing, and deleting teams
- Loading and empty states with call-to-action for first team creation
- Basic access control UI
- Integration with backend API from Phase A
- Global navigation integration

**NOT included:**
- Backend implementation (Phase A)
- Team member management (Phase C and D)
- Advanced filtering/sorting
- Pagination and search functionality

**Dependencies:**
- Phase A must be completed first (backend API must be available)

**IMPORTANT:**
Do not modify any backend code or implement team member functionality in this phase.  

## 1 Create teams page route and add to global navigation

[ ] 1.1 Create `/admin/teams` page route
  - Set up basic page structure with layout components aligned with other pages
  - Add appropriate page title and description
  - Add a breadcrumb to the page

[ ] 1.2 Add teams page to SharedMenu global navigation
  - Add menu item with appropriate icon and translation
  - Place in the Organization section of the navigation

## 2 Create team table component

[ ] 2.1 Fetch teams data using `GET /api/account-management/teams`
  - Use standard TanStack Query for data fetching
  - Handle loading state while data is being fetched

[ ] 2.2 Build team table UI to display team data
  - Create table with columns for team name and description
  - Add placeholder columns for number of members and team admin(s)
  - Follow React Aria Components patterns from existing tables
  - Sorting and multi-selection should not be supported

## 3 Create add team dialog component

[ ] 3.1 Create dialog component for adding new teams
  - Use React Aria Components ensuring dialog aligns with other dialogs in the app
  - Include form with name and multi-line description fields
  - Use standard logic for server side input validation used in other dialogs

[ ] 3.2 Connect dialog to API endpoint
  - Use `POST /api/account-management/teams` for team creation
  - Use standard conventions for showing saving state
  - Use standard conventions for form validation and error handling

## 4 Add team creation functionality

[ ] 4.1 Add "Create team" button to teams page
  - Disable button for all but tenant admin and tenant owner users
  - Open the add team dialog when clicked

[ ] 4.2 Handle successful team creation
  - Close dialog and show success feedback
  - Standard query invalidation logic will automatically refresh the team list

## 5 Create team details side pane

[ ] 5.1 Build the team details side pane component
  - Create a side pane that slides in from the right side of the screen
  - Show team name and description in read-only mode initially
  - List team members and admins will be added in a later phase
  - Fetch team details using `GET /api/account-management/teams/{teamId}`

[ ] 5.2 Implement side pane opening logic
  - Make rows in the team table clickable to open the team details side pane
  - Update URL with team ID parameter to support direct linking
  - Handle loading state while fetching team details

## 6 Add team editing functionality in side pane

[ ] 6.1 Create edit mode for team details
  - When name or description is clicked, switch to inline edit mode
  - Add inline save/cancel buttons
  - Only users with appropriate permissions should be able to change to inline edit mode

[ ] 6.2 Connect edit functionality to API
  - Use `PUT /api/account-management/teams/{teamId}` for team updates
  - Show validation errors inline with the form fields
  - Return to read-only mode after successful update
  - Standard query invalidation logic will automatically refresh the team data

## 7 Add team deletion functionality

[ ] 7.1 Add delete button to team details side pane
  - Add delete button to the side pane
  - Disable delete button for users without appropriate permissions (tenant owner/admin only)

[ ] 7.2 Create confirmation dialog for team deletion
  - Show warning about the consequences of deletion
  - Include confirm and cancel buttons
  - Open dialog when delete button is clicked in side pane

[ ] 7.3 Connect deletion dialog to API
  - Call `DELETE /api/account-management/teams/{teamId}` on confirmation
  - Handle success and error states appropriately
  - Close side pane and refresh team list after successful deletion

## 8 Add empty and loading states

[ ] 8.1 Create loading state for teams table
  - Show appropriate loading indicator while fetching data
  - Maintain consistent layout during loading

[ ] 8.2 Create empty state for when no teams exist
  - Hide table when no teams exist
  - Design informative empty state with helpful message
  - Include prominent call-to-action to create first team
  - Ensure good user experience for new tenants
