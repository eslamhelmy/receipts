# Product Guidelines

## Core Principles
- **Professional & Reassuring:** All system interactions, logs, and public messages must convey reliability and trust.
- **Robustness & Data Integrity:** We prioritize the safety and accuracy of financial data above all else. Transactions must be atomic, and failure states must be handled without data loss.
- **High Performance:** The system architecture must support high throughput and concurrent processing without compromising stability.

## API Standards
- **Actionable Feedback:** API responses, especially errors, must provide clear context and actionable information to help the client application resolve issues (e.g., "File format not supported. Please upload a JPEG or PNG.").
- **Consistency:** Follow established RESTful conventions for status codes, routing, and response structures.

## Development Values
- **Security First:** Always validate inputs and handle sensitive data securely.
- **Test-Driven:** Critical financial logic must be covered by comprehensive unit and integration tests.
