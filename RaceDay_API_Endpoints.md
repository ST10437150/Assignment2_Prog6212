# RaceDay - API Endpoint Plan

## Authentication

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| POST | /api/auth/register | Creates a new user account as either an Organiser or a Participant | None (public) | { name, email, password, role } | 201 Created - user object (no password) / 409 Conflict - email already registered |
| POST | /api/auth/login | Authenticates a user and returns a token for subsequent requests | None (public) | { email, password } | 200 OK - JWT token / 401 Unauthorized - invalid credentials |

## User Profile

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| GET | /api/users/{id} | Returns the profile details of the logged-in user | Any (logged in) | None | 200 OK - user object / 404 Not Found |
| PUT | /api/users/{id} | Updates the logged-in user's own profile details | Any (logged in) | { name, email } | 200 OK - updated user / 400 Bad Request |

## Events

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| GET | /api/events | Returns a list of all upcoming events for participants to browse | None (public) | None | 200 OK - array of events |
| GET | /api/events/{id} | Returns full details of a specific event, including its categories | None (public) | None | 200 OK - event object / 404 Not Found |
| POST | /api/events | Creates a new event | Organiser | { name, eventDate, location, routeDescription } | 201 Created - event object |
| PUT | /api/events/{id} | Updates the details of an existing event | Organiser | { name, eventDate, location, routeDescription } | 200 OK - updated event / 404 Not Found |
| DELETE | /api/events/{id} | Deletes an event created by the organiser | Organiser | None | 204 No Content / 404 Not Found |

## Categories

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| GET | /api/events/{eventId}/categories | Returns all categories (e.g. 5km, 10km) belonging to a specific event | None (public) | None | 200 OK - array of categories |
| POST | /api/events/{eventId}/categories | Adds a new category to an event | Organiser | { name, distanceKm } | 201 Created - category object |
| PUT | /api/categories/{id} | Updates an existing category | Organiser | { name, distanceKm } | 200 OK - updated category / 404 Not Found |
| DELETE | /api/categories/{id} | Removes a category from an event | Organiser | None | 204 No Content / 404 Not Found |

## Event Enrolments

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| POST | /api/categories/{categoryId}/enrol | Enrols the logged-in participant into a chosen category of an event | Participant | { } (participant identified via token) | 201 Created - enrolment object / 409 Conflict - already enrolled |
| GET | /api/users/{id}/enrolments | Returns all events/categories the logged-in participant is enrolled in | Participant | None | 200 OK - array of enrolments |
| GET | /api/events/{eventId}/enrolments | Returns all participants enrolled in a specific event | Organiser | None | 200 OK - array of enrolments |
| DELETE | /api/enrolments/{id} | Cancels a participant's enrolment | Participant | None | 204 No Content / 404 Not Found |

## Results

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
|---|---|---|---|---|---|
| POST | /api/enrolments/{enrolmentId}/results | Captures the finish time and position for a participant's enrolment | Organiser | { finishTime, position } | 201 Created - result object |
| GET | /api/users/{id}/results | Returns the personal performance history of the logged-in participant | Participant | None | 200 OK - array of results |
| GET | /api/events/{eventId}/results | Returns all results for a given event | Organiser | None | 200 OK - array of results |
