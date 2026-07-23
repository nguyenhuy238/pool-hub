# Deposit Refund UAT Checklist

Use this checklist on development/test only before production rollout.

## Public Customer

| Case | Precondition | Steps | Expected result | Actual | Pass/Fail |
|---|---|---|---|---|---|
| Valid secure link | Refund is PendingCustomerInfo and token not expired | Open `/refunds/{token}` | Refund code, booking code, amount, masked email/phone are visible |  |  |
| Expired link | Token expired | Open link | Clear expired-token message, no method form |  |  |
| Send code | Valid token | Click send verification code | Button disables, countdown starts only after API success |  |  |
| Wrong code | Code sent | Enter wrong code + correct phone last4 | Friendly validation error |  |  |
| Expired code | Code expired | Submit expired code | Friendly expired-code error |  |  |
| Wrong phone last4 | Code valid | Enter wrong phone last4 | Verification rejected |  |  |
| Bank mismatch | Verified customer | Enter different account numbers | Frontend blocks submit |  |  |
| Submit bank | Verified customer | Submit bank method | Status becomes PendingApproval, only last4 shown |  |  |
| Submit cash | Verified customer | Choose CashAtVenue | Status becomes PendingApproval, no bank fields required |  |  |
| Track status | Refund status changed by staff | Refresh public page | Vietnamese status reflects latest backend state |  |  |

## Manager

| Case | Precondition | Steps | Expected result | Actual | Pass/Fail |
|---|---|---|---|---|---|
| List refunds | Manager/Admin logged in | Open management refund page | Refunds list with filters and pagination |  |  |
| Approve | Refund PendingApproval | Approve with confirmation | Status becomes Approved |  |  |
| Reject requires reason | Refund PendingApproval | Reject with empty reason | UI blocks or backend error is clear |  |  |
| Request update | Refund PendingApproval | Enter reason and request update | Status returns PendingCustomerInfo, new link sent |  |  |
| Bank data separation | Manager role | Open detail | Full bank account is not exposed unless policy allows |  |  |

## Cashier

| Case | Precondition | Steps | Expected result | Actual | Pass/Fail |
|---|---|---|---|---|---|
| Mark processing | BankTransfer refund Approved | Click start processing | Status becomes Processing |  |  |
| View bank info | Processing BankTransfer | Click view bank info | Warning shown, full info returned only for authorized cashier/admin |  |  |
| Complete bank | Processing BankTransfer | Enter transfer code and confirm | Status Succeeded, RefundedAmount increases |  |  |
| Fail bank | Processing BankTransfer | Enter failure reason | Status Failed, RefundedAmount unchanged |  |  |
| Prepare cash | CashAtVenue refund Approved | Prepare pickup | Status ReadyForCashPickup, pickup code emailed |  |  |
| Wrong pickup code | ReadyForCashPickup | Enter wrong code | Clear error, no state change |  |  |
| Expired pickup code | Code expired | Complete pickup | Clear error, no state change |  |  |
| Complete cash once | ReadyForCashPickup | Enter correct code, booking code, phone last4 | Status Succeeded, RefundedAmount increases once |  |  |
| Complete cash twice | Refund already Succeeded | Submit complete again | Blocked, no second RefundedAmount increment |  |  |

## Finance

| Case | Precondition | Steps | Expected result | Actual | Pass/Fail |
|---|---|---|---|---|---|
| Deposit < Invoice | Deposit paid less than invoice total | Close session | Invoice PartiallyPaid, remaining due shown |  |  |
| Deposit = Invoice | Deposit equals invoice total | Close session | Invoice Paid, no excess refund |  |  |
| Deposit > Invoice pending | Deposit greater than invoice total | Close session | Invoice Paid, paidAmount capped at grandTotal, excess refund PendingCustomerInfo |  |  |
| Pending refund display | Excess refund pending | Open invoice/booking/session detail | Shows "Tiền đang chờ hoàn", not "đã hoàn" |  |  |
| Succeeded refund display | Cashier completed refund | Open details | Shows "Tiền đã hoàn thực tế" |  |  |
| Forfeited deposit | Cancel under 120 minutes or no-show | Open details | Shows forfeited amount, no refund request |  |  |
