# Spec Delta

## ADDED Requirements

### Requirement: Only owners see member management controls
The project page SHALL show the controls to add a member, add or remove a role, and remove a member only when the signed-in person holds `Owner` in that project. Other members SHALL see the same member list without those controls.

#### Scenario: Owner opens the project
- **WHEN** a person who holds `Owner` in a project opens its page
- **THEN** the add-member action and each member's role and remove controls are available

#### Scenario: Member without Owner opens the project
- **WHEN** a person who holds only `Member` in a project opens its page
- **THEN** the members and their role badges are shown, and no add, change-role or remove control is shown

#### Scenario: Owner role is lost
- **WHEN** the signed-in person's `Owner` role in the project is removed and the project is reloaded
- **THEN** the management controls are no longer shown

### Requirement: Report forbidden membership actions
When the server refuses a membership action as forbidden, the project page SHALL tell the person that only owners can manage members, and SHALL refresh the member list so it shows the server's current state.

#### Scenario: Action refused as forbidden
- **WHEN** a membership change is rejected as forbidden
- **THEN** the person sees a message that only project owners can manage members, and the member list shows the server's current state

### Requirement: Explain why the last owner cannot be removed
When the server refuses a change because it would leave the project without an `Owner`, the project page SHALL show the server's explanation and SHALL leave the member list unchanged.

#### Scenario: Remove the only owner
- **WHEN** an owner tries to remove the only `Owner` of the project, or that owner's `Owner` role
- **THEN** the person sees a message that a project must keep at least one owner, and the member still holds `Owner`
