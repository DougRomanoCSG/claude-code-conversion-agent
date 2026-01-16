# Customer API Controller - Method Comparison

**Date**: 2026-01-15
**Purpose**: Compare existing API controller methods vs generated template methods

---

## 📊 Method Comparison

### Existing API Controller (5 methods) - ❌ Uses IUnitOfWork (WRONG PATTERN)

| # | Method | Route | Purpose |
|---|--------|-------|---------|
| 1 | ListPost | POST /api/customer/customerFilter | Search/filter customers |
| 2 | Post | POST /api/customer | Create customer |
| 3 | Put | PUT /api/customer/{id} | Update customer |
| 4 | Get | GET /api/customer/{id} | Get customer by ID |
| 5 | SetActive | PUT /api/customer/{id}/active/{isActive} | Set active status |

**Architecture Issue**:
```csharp
private readonly IUnitOfWork _unitOfWork;  // ❌ WRONG - Controllers should NOT inject IUnitOfWork
```

---

### Generated Template (14 methods) - ✅ Uses ICustomerService (CORRECT PATTERN)

| # | Method | Route | Purpose |
|---|--------|-------|---------|
| 1 | Search | POST /api/customer/search | Search customers (DataTables) |
| 2 | GetById | GET /api/customer/{id} | Get customer by ID |
| 3 | GetByAccountingSyncId | GET /api/customer/accounting/{accountingSyncId} | Get customer by accounting sync ID |
| 4 | Create | POST /api/customer | Create customer |
| 5 | Update | PUT /api/customer/{id} | Update customer |
| 6 | Delete | DELETE /api/customer/{id} | **Delete customer** ⭐ NEW |
| 7 | GetContacts | GET /api/customer/{id}/contacts | **Get customer contacts** ⭐ NEW |
| 8 | CreateContact | POST /api/customer/contacts | **Create contact** ⭐ NEW |
| 9 | UpdateContact | PUT /api/customer/contacts/{id} | **Update contact** ⭐ NEW |
| 10 | DeleteContact | DELETE /api/customer/contacts/{id} | **Delete contact** ⭐ NEW |
| 11 | GetBargeExTransactions | GET /api/customer/{id}/bargex-transactions | **Get BargeEx transactions** ⭐ NEW |
| 12 | CreateBargeExTransaction | POST /api/customer/bargex-transactions | **Create BargeEx transaction** ⭐ NEW |
| 13 | UpdateBargeExTransaction | PUT /api/customer/bargex-transactions/{id} | **Update BargeEx transaction** ⭐ NEW |
| 14 | DeleteBargeExTransaction | DELETE /api/customer/bargex-transactions/{id} | **Delete BargeEx transaction** ⭐ NEW |

**Architecture**:
```csharp
private readonly ICustomerService _customerService;  // ✅ CORRECT - Controllers inject Services
```

---

## 🆚 Overlapping Functionality

| Existing Method | Generated Method | Notes |
|-----------------|------------------|-------|
| ListPost (POST /customerFilter) | Search (POST /search) | Both do customer search/filter |
| Get (GET /{id}) | GetById (GET /{id}) | Same functionality, different names |
| Post (POST /) | Create (POST /) | Same functionality, different names |
| Put (PUT /{id}) | Update (PUT /{id}) | Same functionality, different names |

---

## ✨ NEW Functionality in Generated Template

### 1. GetByAccountingSyncId
```csharp
GET /api/customer/accounting/{accountingSyncId}
```
Get customer by accounting sync ID (for integrations)

### 2. Delete Customer
```csharp
DELETE /api/customer/{id}
```
Delete customer (existing only has SetActive)

### 3. Contact Management (4 endpoints)
```csharp
GET    /api/customer/{id}/contacts
POST   /api/customer/contacts
PUT    /api/customer/contacts/{id}
DELETE /api/customer/contacts/{id}
```
Full CRUD for customer contacts

### 4. BargeEx Transaction Management (4 endpoints)
```csharp
GET    /api/customer/{id}/bargex-transactions
POST   /api/customer/bargex-transactions
PUT    /api/customer/bargex-transactions/{id}
DELETE /api/customer/bargex-transactions/{id}
```
Full CRUD for BargeEx transactions

---

## 🎯 Recommendation

### Option A: Replace Entire Controller (Recommended)
**Why**:
- ✅ Fixes architectural violation (IUnitOfWork → ICustomerService)
- ✅ Adds 9 new endpoints (GetByAccountingSyncId, Delete, Contacts, BargeEx)
- ✅ Follows correct layering pattern
- ⚠️ Changes endpoint URLs (e.g., /customerFilter → /search)

**Impact**: UI might need to update API calls if it uses:
- POST /api/customer/customerFilter → POST /api/customer/search
- Need to verify UI doesn't directly call these endpoints

### Option B: Merge New Methods Only
Keep existing 5 methods, add 9 new methods:
- GetByAccountingSyncId
- Delete
- Contact endpoints (4)
- BargeEx endpoints (4)

**Issue**: Would have BOTH patterns in same controller:
```csharp
private readonly IUnitOfWork _unitOfWork;      // Used by existing 5 methods
private readonly ICustomerService _customerService;  // Used by new 9 methods
```
This is messy but functional.

### Option C: Fix Architecture First
1. Create ICustomerService
2. Move repository logic to CustomerService
3. Refactor existing 5 methods to use ICustomerService
4. Then add 9 new methods

**Duration**: 4-6 hours of refactoring

---

## 📋 Decision Needed

**Which option do you prefer?**

1. **Replace entire controller** (cleanest, but may break UI calls)
2. **Merge with mixed patterns** (quick, but violates architecture)
3. **Refactor then merge** (correct, but takes time)

---

## 🔧 Next Steps After Decision

### If Option 1 (Replace):
```bash
# Backup existing
cp CustomerController.cs CustomerController.cs.backup

# Replace with generated template
cp output/Customer/Templates/api/Controllers/CustomerController.cs \
   C:/Dev/BargeOps.Admin.Mono/src/BargeOps.API/src/Admin.Api/Controllers/CustomerController.cs

# Check UI for breaking changes
grep -r "customerFilter" C:/Dev/BargeOps.Admin.Mono/src/BargeOps.UI/
```

### If Option 2 (Merge):
```bash
# Run interactive merge agent
bun run agents/interactive-template-merge.ts --entity "Customer"
```

### If Option 3 (Refactor):
1. Create ICustomerService interface
2. Create CustomerService implementation
3. Register in DI
4. Refactor existing methods
5. Then merge new methods

---

**Status**: Awaiting decision
**Author**: Interactive Merge Agent Analysis
