# Spec Delta

## ADDED Requirements

### Requirement: Admin is not a project role
The system SHALL refuse to give a user the `Admin` role in a project. The refusal MUST be reported as invalid, and nothing is recorded. Holding `Admin` globally SHALL NOT make a user a member of any project or give them any project role.

#### Scenario: Owner tries to assign Admin in a project
- **WHEN** an `Owner` of a project gives a user the `Admin` role in that project
- **THEN** the system rejects the request as invalid and records nothing

#### Scenario: Admin is not automatically a project member
- **WHEN** a user who holds `Admin` but no role in project P lists projects
- **THEN** P is not in the list
