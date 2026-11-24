
# MiPos - Business Rules

## 1. Product Overview

MiPos is a point-of-sale and business management system initially
focused on restaurants, fast-food establishments, and small businesses
in Colombia.

The main objective is to simplify daily commercial operations:

- Product management
- Order management
- Restaurant table management
- Sales and payments
- Cash register management
- Inventory management
- Electronic billing
- Printing
- Business reporting

The system must be simple to operate, reliable, and capable of
working locally without internet access.

## 2. Business Organization

The organizational structure consists of:

- Business
- Branch
- Cash register
- Device
- User

A business may have multiple branches.

A branch may have multiple cash registers and devices.

The initial MVP targets businesses with one branch and one
cash register, without restricting future expansion.

Devices may represent:

- Cash register computers
- Waiter tablets or phones
- Kitchen devices
- Administration computers

Devices are not necessarily associated with a cash register.

Business and branch data must remain properly isolated.

The initial market uses COP and the America/Bogota time zone.

## 3. Operation Modes

MiPos supports two initial operation modes.

### Cash Register Only

The cashier creates orders, registers products, receives payments,
and completes sales.

Waiter-specific workflows are not required.

### Cash Register and Waiters

Waiters create and manage customer orders.

The cashier receives payments and completes sales.

Waiters and cashiers operate against the same local backend.

Table management can be enabled independently at branch level.

## 4. Users, Roles and Permissions

Each user belongs to a business and has one role.

A role may contain multiple permissions.

Initial roles may include:

- Administrator
- Cashier
- Waiter
- Supervisor

Permissions determine the operations available to each user.

Examples:

- Manage products
- Create orders
- Modify orders
- Cancel order items
- Register payments
- Open cash registers
- Close cash registers
- Register cash movements
- View reports
- Manage fiscal documents
- Manage users

Permissions must be enforced by the backend, not only by the UI.

Individual branch assignments are not part of the initial
authorization model.

## 5. Typologies

Typologies provide centralized classifications used by the system.

Initial groups include:

- OPERATION_MODE
- DEVICE_TYPE
- IDENTIFICATION_TYPE
- PRODUCT_TYPE
- UNIT_OF_MEASURE
- ORDER_TYPE
- CASH_REGISTER_MOVEMENT_TYPE
- INVENTORY_MOVEMENT_TYPE
- PRINTER_CONNECTION_TYPE
- PRINT_DOCUMENT_TYPE

Groups and codes are controlled by MiPos.

Users must not create arbitrary system typologies.

Referenced typologies must belong to the expected group.

Existing historical references must remain valid when a typology
is deactivated.

## 6. Product Catalog

Products belong to a business.

The catalog supports:

- Categories
- Products
- Services
- Combos
- Product images
- Product codes
- Optional SKUs
- Optional barcodes
- Units of measure
- Prices
- Availability
- Inventory configuration

Only active and sellable products may be added to new orders.

Products must not be permanently removed when they are referenced
by historical transactions.

Catalog changes must not modify historical sales.

Product images must remain accessible during offline operation.

### Categories

Categories organize products within the sales interface.

Examples:

- Lunches
- Beverages
- Side dishes
- Fast food

Categories are configurable by each business.

### Prices

Products may have prices associated with price lists.

The initial MVP uses a default price list.

The database supports multiple price lists, but advanced price
list management is not initially required.

The price assigned to an order item is preserved when the item
is added to the order.

Later catalog price changes must not modify existing order items.

## 7. Combos

A combo is a sellable product composed of other products.

Each combo has:

- Name
- Price
- Components
- Quantity of each component
- Active status

The combo has its own selling price.

The selling price is not necessarily the sum of its components.

The initial implementation supports fixed combo compositions.

Nested combos are not supported.

The composition must be preserved when a combo is added
to an order.

Changes to a combo must not modify previous orders or sales.

Dynamic component selection and substitutions are outside
the initial scope.

## 8. Order Types

The system supports three initial order types:

- DINE_IN
- TAKEAWAY
- DELIVERY

Each business may enable or disable the available order types.

### Dine In

The customer consumes products inside the establishment.

The order may be associated with a restaurant table.

Individual products within a dine-in order may be marked
as takeaway.

### Takeaway

The customer purchases products to consume elsewhere.

A restaurant table is not required.

### Delivery

The order is intended for delivery outside the establishment.

Advanced delivery management is outside the initial scope:

- Delivery routes
- Driver management
- Address management
- Delivery tracking
- Automated delivery fees

## 9. Restaurant Tables

Tables belong to a branch.

Each table may have:

- Code
- Name
- Optional capacity
- Display order
- Active status

A table is considered occupied when it has an open order.

A table is considered available when it has no open order.

Table occupancy must be derived from orders rather than maintained
as an independent duplicated status.

The initial MVP supports one open order per table.

The system must prevent concurrent creation of multiple open
orders for the same table.

The table becomes available after its order is closed or canceled.

The number of tables must be configurable.

The interface must support establishments with at least 30 tables
without requiring database changes.

## 10. Orders

An order represents a customer's current consumption before
the commercial sale is completed.

Initial order statuses:

- OPEN
- CLOSED
- CANCELED

An open order may:

- Add products
- Modify product quantities
- Cancel items
- Include notes
- Include combos
- Include takeaway items
- Be sent to the kitchen
- Be associated with a table

Sending an order to the kitchen does not close it.

Opening an order does not require an open cash register session.

Payment and completion through the POS require a valid
cash register session.

Each order item must preserve the relevant product information:

- Product code
- Product name
- Quantity
- Unit of measure
- Unit price
- Taxes
- Discounts
- Total
- Combo composition, when applicable

Canceled items must not contribute to the order total.

An order must not generate more than one normal sale.

Closed or canceled orders cannot be modified through the
normal order editing workflow.

## 11. Sales

A sale represents a completed commercial transaction.

A sale may originate from:

- A restaurant order
- A direct sale without a restaurant order

The initial sales workflow uses immediate payment.

Credit sales are supported by the initial database design
but are not part of the initial implementation.

A sale must preserve:

- Products sold
- Quantities
- Historical prices
- Discounts
- Taxes
- Additional charges
- Total amount
- Payments
- Responsible user
- Branch
- Cash register session
- Transaction date

Historical sales must not change when products, prices,
customers, or payment methods are modified.

Confirmed sales must not be physically deleted.

Corrections, cancellations, and returns must use explicit
business operations that preserve transaction history.

The commercial sale status is independent of fiscal
document and printing statuses.

## 12. Payments

Initial payment methods:

- CASH
- BANK_TRANSFER
- CARD

Each business may configure its enabled payment methods.

Payment records must preserve the payment method information
used at the time of the transaction.

The database supports multiple payments per sale.

The initial checkout interface may begin with a single
payment method per sale.

For an immediate-payment sale, the net approved payments
must cover the total sale amount.

### Cash

When receiving cash:

- The cashier enters the amount received.
- The system calculates the change.
- The amount received must cover the cash payment amount.
- The payment amount represents the net cash collected.

Example:

    Sale total:     25,000 COP
    Cash received:  30,000 COP
    Change:          5,000 COP
    Net payment:    25,000 COP

### Bank Transfer

The cashier registers the received payment.

Bank transfers do not increase the physical cash balance.

A reference may be required depending on configuration.

Automatic bank verification is outside the initial scope.

### Card

The cashier registers the card payment.

Card payments do not increase the physical cash balance.

Direct integration with payment terminals is not required
for the initial MVP.

Payment reversals must preserve the original payment record.

## 13. Cash Register Management

A cash register represents a physical or logical checkout point.

Each cash register may have multiple sessions over time.

Only one session may be open per cash register at a time.

### Opening

Opening a cash register session requires:

- Responsible user
- Cash register
- Opening date and time
- Initial cash amount

### Cash Movements

During an open session, authorized users may register:

- Manual income
- Withdrawals
- Expenses
- Positive adjustments
- Negative adjustments

Each movement must include:

- Amount
- Movement type
- Reason
- Responsible user
- Operation date

Sales payments must not be duplicated as manual
cash register movements.

### Closing

Closing a cash register session must provide:

- Opening cash amount
- Sales totals
- Totals by payment method
- Cash income
- Cash withdrawals and expenses
- Expected physical cash
- Declared physical cash
- Cash difference

Expected physical cash is calculated as:

    Opening cash
    + Net approved cash payments
    + Manual cash income
    - Cash withdrawals and expenses
    +/- Cash adjustments

Cash payment reversals and movement corrections must be
included in the corresponding net amounts.

The closing difference is calculated as:

    Declared cash - Expected cash

Card and transfer payments must appear in the closing
summary but must not increase the physical cash balance.

All POS payments must be associated with the corresponding
cash register session for reporting purposes.

A closed session cannot receive new normal operations.

## 14. Additional Charges

The database supports configurable additional charges.

A charge may use:

- Fixed amount
- Percentage

The applied charge and its calculated amount must be
preserved historically.

The exact rules for calculation bases, taxes, packaging,
delivery fees, and optional service charges must be defined
before implementing this functionality.

Do not assume that every charge applies automatically.

## 15. Customers

Registering a customer is not mandatory for a normal sale.

Anonymous consumer sales must be supported.

Customer information may include:

- Identification type
- Identification number
- Name
- Legal name
- Email
- Phone number

Customer information may be required when requesting
a personalized fiscal document.

Customer relationship management is outside the initial scope.

## 16. Inventory

Inventory is included in the database model but may be
implemented after the initial sales workflows.

Inventory may be enabled or disabled per branch.

Each product may independently indicate whether inventory
tracking is required.

Inventory balances are maintained per product and branch.

The database supports:

- Current quantity
- Reserved quantity
- Minimum quantity
- Inventory movements

Inventory movement types include:

- Inbound
- Outbound
- Adjustment
- Sale
- Return
- Transfer

Inventory movements must preserve the previous and resulting
quantities.

Balance changes and their associated movements must be
persisted transactionally.

The following rules require further definition before
implementing inventory:

- Stock deduction timing
- Negative inventory policy
- Product reservations
- Combo component deductions
- Ingredient and recipe management

Do not implement advanced warehouse or purchasing
functionality without explicit requirements.

## 17. Electronic Billing

MiPos is designed to integrate with Factus for Colombian
electronic billing.

The database supports:

- Fiscal configuration
- Fiscal numbering ranges
- Electronic invoices
- Electronic POS documents
- Credit notes
- Fiscal document history
- Fiscal document retries

### Fiscal Configuration

Each business may configure its fiscal integration.

The initial product management interface does not require
the user to configure tax codes and rates individually
for every product.

Products may inherit the business fiscal configuration.

Product-specific tax configuration remains supported
by the database.

### Document Request

During checkout, the user may request a document issued
with customer identification information.

The UI may distinguish between:

- Natural person
- Company

The required identification and fiscal information must
be validated according to the document type.

Not requesting a personalized document does not imply
that fiscal issuance is unnecessary.

### Fiscal Document Lifecycle

Fiscal documents have an independent lifecycle:

- PENDING
- PROCESSING
- ISSUED
- REJECTED
- UNKNOWN
- PERMANENT_ERROR

Fiscal documents must preserve the information submitted
to the provider.

Retry operations must prevent duplicate fiscal issuance.

Credit notes must reference the original fiscal document.

The fiscal numbering range cache does not independently
assign official fiscal numbers.

### Offline Operation

Core POS operations must not depend on the availability
of the fiscal provider.

The system must preserve the commercial transaction
and record the corresponding fiscal situation.

Issuance, contingency handling, document selection,
submission deadlines, and credit note requirements must
be validated against applicable DIAN and Factus requirements
before production implementation.

Do not assume that unrestricted deferred issuance is
legally permitted.

## 18. Printing

The system supports:

- Sales receipts
- Kitchen tickets

Printers belong to a branch.

Supported connection types:

- Network printers
- Operating system print queues

Printing configuration determines:

- Printer destination
- Document type
- Number of copies
- Automatic or manual printing

Different document types may use different printers.

A printing failure must not cancel a completed sale.

Authorized users may request manual reprints.

Printing jobs must preserve the original document content.

An uncertain printing result must not automatically generate
duplicate physical documents.

A commercial receipt is not necessarily a legally valid
fiscal document.

## 19. Sales History and Reporting

The initial reporting functionality includes:

- Sales history
- Sales by date
- Sale details
- Payment method summaries
- Cash register closings
- Best-selling products

Sales history must provide access to:

- Products sold
- Quantities
- Prices
- Discounts
- Taxes
- Payments
- Sale status
- Fiscal document status

Fiscal document retry and issuance actions may be available
from the corresponding sale when applicable.

Reports must respect business and branch boundaries.

Historical reports must use transaction information instead
of current catalog prices.

Date filters must use the business or branch time zone.

## 20. Offline Operation and Backups

MiPos initially operates using a local backend and a local
PostgreSQL database.

The following operations must not require internet access:

- Authentication for existing local users
- Product management
- Order management
- Table management
- Recording payments
- Completing local commercial transactions
- Cash register management
- Local reporting

Multiple devices may connect through the restaurant's
local network.

Internet-dependent services include:

- Electronic billing communication
- Remote backups
- Future external integrations

The initial backup strategy consists of periodic backups
uploaded to a remote destination when connectivity is available.

Backups must include the database and relevant local assets,
such as product images.

The backup frequency, storage provider, retention policy,
encryption, and restoration procedures remain pending.

Bidirectional synchronization is not part of the
initial architecture.

## 21. Initial Implementation Priorities

### Priority 1 - Foundation

- Business
- Branch
- Typologies
- Authentication
- Users
- Roles
- Permissions

### Priority 2 - Catalog

- Categories
- Products
- Default price list
- Combos

### Priority 3 - Restaurant Operations

- Order types
- Restaurant tables
- Orders
- Order items

### Priority 4 - Sales and Cash Registers

- Cash register opening
- Payments
- Sale completion
- Cash movements
- Cash register closing
- Sales history

### Priority 5 - Additional Functionality

- Inventory
- Printing
- Factus integration
- Fiscal document management
- Reporting extensions
- Automated backups

These priorities define an incremental implementation order,
not separate applications or deployments.

## 22. Out of Scope Initially

Do not implement the following without explicit requirements:

- Microservices
- Cloud SaaS platform
- Bidirectional database synchronization
- Advanced delivery logistics
- Customer loyalty programs
- Full accounting software
- Payroll
- Advanced purchasing management
- Multiple warehouses
- Advanced restaurant reservations
- Dynamic combo substitutions
- Automated bank reconciliation
- Advanced food safety management

The database may anticipate future capabilities without
requiring their immediate implementation.

## 23. General Business Principles

- Preserve historical commercial information.
- Do not silently modify completed transactions.
- Prevent duplicate sales and payments.
- Enforce valid operation states.
- Validate business ownership of resources.
- Keep commercial, fiscal, and printing lifecycles independent.
- Preserve consistency in concurrent operations.
- Prioritize operational simplicity.
- Do not invent undocumented business requirements.
- Implement functionality incrementally.
