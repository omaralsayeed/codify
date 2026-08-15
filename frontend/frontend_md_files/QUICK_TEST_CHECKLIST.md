# ⚡ Quick Test Checklist — Do This Now

> **Frontend:** http://localhost:4200  
> **Backend:** http://localhost:5237

---

## 🚀 Start Testing (5 minutes)

### Step 1: Open Frontend
```
Browser: http://localhost:4200
```

What you should see:
- [ ] Codify home page loads
- [ ] No console errors
- [ ] "Sign up" and "Sign in" buttons visible

---

### Step 2: Try to Register

**Form:**
- Full Name: `Test Student`
- Email: `teststudent@example.com`
- Password: `password123` (8+ chars)
- Role: `Student`

**After clicking Register, you should see:**
- [ ] No console errors
- [ ] Redirect to `/problems` page
- [ ] Avatar with initials "TS" in top right
- [ ] Token in localStorage:
  ```javascript
  localStorage.getItem('codify_token')  // should exist
  ```

**If you see an error:**
- Go to Network tab in DevTools
- Find the failed request
- Copy the error message and share it

---

### Step 3: Check Problems List

On the `/problems` page, you should see:
- [ ] Table with problems loading
- [ ] Difficulty shows `Easy`, `Medium`, `Hard` (NOT `0`, `1`, `2`)
- [ ] Topic shows text (NOT empty)
- [ ] At least 3 problems visible
- [ ] No loading spinner

**If problems don't load:**
- Go to Network tab
- Look for request to `/api/problems`
- Check if it's `200 OK` or has an error
- Copy the error

---

### Step 4: Click a Problem

Click on any problem title in the table.

You should see:
- [ ] Page loads the specific problem
- [ ] Title matches what you clicked
- [ ] Difficulty badge shows correct text
- [ ] Description appears (problem statement)
- [ ] Examples show with Input/Output
- [ ] Constraints listed
- [ ] No loading spinner

**If problem doesn't load:**
- Check browser Network tab for error on `/api/problems/{id}`
- Check console for JavaScript errors
- Copy any error messages

---

### Step 5: Verify Data is Real

The problems and their details should be **real data from your backend database**, not the old hardcoded "Two Sum" example.

**Check:**
- [ ] Problem titles are NOT all "Two Sum"
- [ ] Each problem has different description
- [ ] Examples are specific to each problem
- [ ] Constraints vary per problem

---

## ✅ Success Indicators

All of these should be true:

- [x] Registration works and auto-logs you in
- [x] You see real problems from the database
- [x] Difficulty shows as text (`Easy`), not numbers (`0`)
- [x] Problem detail loads when you click a problem
- [x] All data is real and varies per problem
- [x] No errors in browser console
- [x] No CORS errors

---

## ❌ Common Issues & Quick Fixes

### Issue: "Cannot reach backend"

**Error message:** `CORS error` or `connection refused`

**Fix:**
1. Check backend is running at http://localhost:5237
2. Try opening http://localhost:5237/swagger in browser
3. If that fails, start the backend first

---

### Issue: "Difficulty shows 0, 1, 2 instead of Easy, Medium, Hard"

**Indicates:** Enum mapping not working

**Check:**
1. Browser console for errors
2. That `mapDifficulty()` is imported in problem.service.ts
3. Restart frontend: `npm start`

---

### Issue: "Problems don't load, just shows 'Loading...'"

**Indicates:** API call failing silently

**Check:**
1. Network tab → look for `/api/problems` request
2. Check if it's showing as red (error)
3. Check backend is returning JSON with `data` envelope
4. Check browser console for errors

---

### Issue: "Registered but not logged in"

**Indicates:** Register didn't auto-chain to login

**Check:**
1. Network tab → should see TWO requests:
   - POST `/api/auth/register` (201)
   - POST `/api/auth/login` (200)
2. If only one shows, check browser console error
3. Check that `switchMap` is used in register method

---

### Issue: "Page says 'Two Sum' hardcoded content"

**Indicates:** Problem detail not loading from backend

**Check:**
1. Network tab → look for `/api/problems/{id}` request
2. Check if route param is being read from URL
3. Check that `ActivatedRoute` is injected
4. Restart frontend

---

## 🎥 What To Do Next

### If Everything Works ✅

1. Open `TESTING_GUIDE.md` and run full test suite
2. Document results
3. Check all 5 test scenarios
4. You're ready for Sprint 2!

### If Something Fails ❌

1. Use troubleshooting section above
2. Check browser console and Network tab
3. Share the error message and Network response
4. I can help debug

---

## 📱 Browser Console Tips

**Open DevTools:** F12 or Right-click → Inspect

**Check localStorage:**
```javascript
// See your JWT token
localStorage.getItem('codify_token')

// See your user object
console.log(JSON.parse(localStorage.getItem('codify_user')))

// Clear all if you need to reset
localStorage.clear()
```

**Monitor requests:**
- Network tab → Click a problem → should see GET to `/api/problems/{id}`
- Check the response is valid JSON with `data` wrapper

---

## 🔗 Quick Links

| Page | URL |
|---|---|
| Frontend | http://localhost:4200 |
| Register | http://localhost:4200/auth/register |
| Login | http://localhost:4200/auth/login |
| Problems | http://localhost:4200/problems |
| Backend API | http://localhost:5237/api |
| Swagger Docs | http://localhost:5237/swagger |

---

## 📝 If You Find Issues

**What to share:**
1. Error message from browser console
2. Screenshot of error
3. What request failed (check Network tab)
4. Response from failed request (right-click → Copy as cURL)

Then I can help fix it!

---

**Ready? Open http://localhost:4200 and start testing!** 🚀
