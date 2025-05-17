# Dynamic Consistency Boundary (DCB)

## Overview

**Dynamic Consistency Boundary (DCB)** is a modern technique for enforcing consistency in event-driven systems, offering a flexible alternative to traditional transactional boundaries. DCB enables strong consistency where needed—especially for operations spanning multiple entities—while maintaining the scalability and resilience of event-driven architectures.


> For more details, visit the official DCB website: [dcb.events](https://dcb.events/)

---

## What Problem Does DCB Solve?

Traditional event-sourced systems often use aggregates to enforce invariants (e.g., a student cannot enroll in more than 10 courses, or a course cannot have more than N students). However, when business rules span multiple aggregates, enforcing consistency becomes complex and can lead to duplicated events, compensating actions, and temporary inconsistencies.

DCB addresses these challenges by:
- Allowing events to be tagged with multiple entities.
- Using a single event stream per bounded context.
- Enabling queries and consistency checks across multiple entities without complex sagas or distributed transactions.

---

## How DCB Works (with Example)

The image below illustrates DCB in the context of an academic enrollment system:

![DCB Example](./images/dcb-example.png)

### 1. Single Event Stream

All events related to academic enrollment (e.g., course definitions, student registrations, subscriptions) are stored in a single event stream for the bounded context. Each event is tagged with relevant entity identifiers (e.g., `student:s2`, `course:c2`).

### 2. Command Processing Example

Suppose a command is issued: **Student s2 subscribes to Course c2**.

#### Step 1: DCB Query

- The system queries the event stream for events tagged with `student:s2` and `course:c2`.
- This retrieves all relevant events for the student and course, such as registrations and previous subscriptions.

#### Step 2: In-Memory Decision Model

- The system builds an in-memory model to check business rules:
  - Does student s2 exist?
  - Does course c2 exist?
  - How many courses is s2 already subscribed to?
  - How many students are already in c2?

If all invariants are satisfied, the subscription event is appended to the stream, tagged with both `student:s2` and `course:c2`.

---

## Key Benefits

- **Simplicity:** No need for complex sagas or compensating transactions.
- **Consistency:** Enforces cross-entity invariants reliably.
- **Scalability:** Maintains a single event stream per bounded context, supporting high throughput.
- **Flexibility:** Consistency boundaries are defined dynamically, not statically tied to aggregates.

---

## Further Reading

- Official DCB website: [https://dcb.events/](https://dcb.events/)
- Sara Pellegrini's blog post: "Killing the Aggregate"
- [DCB Examples](https://dcb.events/examples/course-subscriptions/)

---

## Visual Explanation

The image above demonstrates:
- How events are tagged and stored in a single stream.
- How DCB queries retrieve all relevant events for decision-making.
- How the in-memory model checks invariants before allowing state transitions.

---

## Getting Started

To implement DCB:
1. Store all events in a single stream per bounded context.
2. Tag events with all relevant entity identifiers.
3. Use queries to build in-memory models for command processing.
4. Enforce consistency using optimistic locking on the query result.

For libraries, specifications, and more examples, visit [dcb.events](https://dcb.events/).

---

**DCB** is a powerful approach for modern event-driven systems, balancing strong consistency with the flexibility and scalability required in distributed architectures.

---

**References:**
- [Dynamic Consistency Boundary (DCB) - Official Site](https://dcb.events/)

---

## Command Processing

Currently, there is no dedicated command processing logic implemented in the main codebase. Instead, command processing—such as handling commands like "Student s2 subscribes to Course c2" and enforcing business rules—is performed within the test cases. The tests simulate command handling by building the in-memory decision model, querying events, and checking invariants as described in the DCB approach.

**Planned Enhancements:**
Future additions to the project are expected to introduce explicit command processing components. These will move the logic from tests into the main application, providing a clearer separation between command handling, event storage, and business rule enforcement.

**Summary:**
- **Current state:** Command processing is only done in tests.
- **Future state:** Command processing will be implemented as part of the main project logic. 