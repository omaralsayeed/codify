# Spec — Pricing Feature (Branch: `pricing`)

> **Created:** August 23, 2026
> **Branch:** `pricing`
> **Status:** 🟡 IN PLANNING — Chunks defined, no code written yet
> **Backend touches:** ZERO — purely frontend

---

## 🎯 Goal

Add a full **Pricing experience** to Codify that:
1. Presents 3 subscription tiers on the landing page with a Monthly/Yearly toggle
2. Integrates real Stripe Payment Links (test mode, no SDK, no backend)
3. Shows the user's active plan as a color-coded badge next to their avatar in the navbar
4. Persists the plan across sessions via localStorage (same pattern as `avatarUrl`)
5. Looks polished and production-grade for the graduation project live demo

---

## ⚠️ Hard Constraints (do not violate)

- **NO backend files touched** — no controllers, services, DTOs, migrations
- **NO auth guards or routing guards modified**
- **NO new npm packages** — emoji/Unicode for icons, match existing project style
- **NO Stripe SDK / stripe-js** — payment links are static `<a href>` tags only
- Only existing-file edit allowed outside new files: one `<app-pricing>` insertion in `HomeComponent` template + navbar additions

---

## 🗂️ Sprint Architecture Tree

```
Sprint: Pricing Feature
│
├── CHUNK 1 — Data Layer (model + auth service)
│   ├── C1-A  Add `plan` field to User model
│   └── C1-B  Add `setPlan()` method to AuthService
│
├── CHUNK 2 — PricingComponent (the big one)
│   ├── C2-A  Create pricing/ folder under home/components/
│   ├── C2-B  pricing.component.ts  — data, toggle logic, Stripe constants
│   ├── C2-C  pricing.component.html — 3 cards, toggle, features, CTAs
│   └── C2-D  pricing.component.scss — layout, themes, animations
│
├── CHUNK 3 — Landing Page Integration
│   ├── C3-A  Add `id="pricing"` anchor to pricing section wrapper
│   └── C3-B  Import + append `<app-pricing>` in HomeComponent (last section)
│
├── CHUNK 4 — Navbar Updates
│   ├── C4-A  Guest nav: add `Pricing` fragment link (desktop + mobile)
│   └── C4-B  Logged-in profile dropdown: "Free Plan" → "→ View Plans" link
│
├── CHUNK 5 — Plan Badge (navbar avatar area)
│   ├── C5-A  Plan badge pill component (inline, no separate file needed)
│   └── C5-B  Wire badge to `auth.user().plan` signal — shows beside avatar
│
└── CHUNK 6 — Stripe Success Redirect Handler
    ├── C6-A  Add `?plan=` query param to Stripe success URLs (instructions)
    └── C6-B  Route listener reads `?plan=` on app load → calls `setPlan()`
```

---

## 📦 Chunk Details

---

### CHUNK 1 — Data Layer
**Files touched:** `user.model.ts`, `auth.service.ts`
**Risk:** Low — additive only, no existing logic changed

- Add to `User` interface:
  ```ts
  plan?: 'free' | 'learner' | 'proplus';
  ```
- Add to `AuthService`:
  ```ts
  setPlan(plan: 'free' | 'learner' | 'proplus'): void
  // patches _currentUser signal + localStorage, same pattern as setAvatarUrl()
  ```

---

### CHUNK 2 — PricingComponent
**Files created:**
```
src/app/features/home/components/pricing/
  ├── pricing.component.ts
  ├── pricing.component.html
  └── pricing.component.scss
```
**Risk:** Medium — largest chunk, zero side effects on other components

#### Stripe Payment Link Constants (top of `.ts` file)
```ts
// ⬇️ REPLACE THESE WITH YOUR REAL STRIPE DASHBOARD LINKS BEFORE DEMO
const STRIPE_LINK_LEARNER_MONTHLY  = 'https://buy.stripe.com/test_REPLACE_ME';
const STRIPE_LINK_LEARNER_YEARLY   = 'https://buy.stripe.com/test_REPLACE_ME';
const STRIPE_LINK_PROPLUS_MONTHLY  = 'https://buy.stripe.com/test_REPLACE_ME';
const STRIPE_LINK_PROPLUS_YEARLY   = 'https://buy.stripe.com/test_REPLACE_ME';
```

#### Plans Data (hardcoded, no backend)

| Plan | Monthly (EGP) | Yearly (EGP) | Highlighted |
|------|--------------|-------------|-------------|
| Free — "Start Solving" | 0 | 0 | ❌ |
| Learner — "Level Up" | 199 | 1990 | ✅ Most Popular |
| Pro Plus — "Go Pro" | 499 | 4990 | ❌ |

#### Toggle Logic
- Pill-style `Monthly | Yearly` toggle, default Monthly
- Toggling updates `billingCycle: 'monthly' | 'yearly'` signal
- Prices + Stripe link targets update reactively
- Yearly: show strikethrough of `monthly × 12`, actual yearly price, discount badge
- CSS `opacity` fade transition on price change (clean, no JS counting lib)

#### Features per Plan (emoji icons, match project style)
**Free:**
- ∞ Unlimited problems
- ▶️ Judge0-powered code execution
- 🕐 Submission history
- ✨ AI hints — `included: false` (greyed)
- 🛡️ Code quality analysis — `included: false` (greyed)

**Learner:**
- ✅ Everything in Free
- ✨ 10 AI hints / month
- 🎯 Hints on up to 5 problems / month
- 🛡️ Basic code quality feedback

**Pro Plus:**
- ✅ Everything in Learner
- ⚡ 300 AI hints / month
- ∞ Unlimited problems using hints
- 🛡️ Full code quality + academic integrity analysis
- 💬 Priority support

#### UI Design Requirements
- 3 cards in a row desktop → stacked mobile
- **Learner card:** `scale(1.03)`, brand-color border (`#3291b9`), "Most Popular" badge
- Free CTA: outline/ghost button → `routerLink="/auth/register"`
- Learner + Pro Plus CTA: solid brand-color button → `<a href="..." target="_blank" rel="noopener">`
- Pro Plus CTA: slight gradient treatment for premium feel
- Trust line below cards: `🔒 Secure payment via Stripe · Cancel anytime`
- Light + dark theme support via existing CSS variables
- Smooth toggle animation (CSS transition, not JS)

---

### CHUNK 3 — Landing Page Integration
**Files touched:** `home.component.ts` only
**Risk:** Very low — one import + one tag added

- Import `PricingComponent` in `HomeComponent`
- Append `<app-pricing id="pricing">` as the last section
- Section order becomes:
  ```
  Hero → Features → HowItWorks → StudentDashboardPreview → InstructorDashboardPreview → Pricing
  ```

---

### CHUNK 4 — Navbar Updates
**Files touched:** `navbar.component.html`, `navbar.component.ts`
**Risk:** Low — additive only

#### C4-A Guest nav — Pricing link
Add to desktop guest links and mobile guest menu:
```html
<a routerLink="/" fragment="pricing" class="nav-link">Pricing</a>
```

#### C4-B Logged-in profile dropdown — Plan link
The profile dropdown currently shows static text `"Free Plan"`.
Replace with dynamic, clickable link:
```html
<div class="profile-plan">
  <a routerLink="/" fragment="pricing" class="profile-plan-link">
    {{ planLabel }} · View Plans →
  </a>
</div>
```
Where `planLabel` is a getter in the component:
```ts
get planLabel(): string {
  const plan = this.auth.user()?.plan;
  if (plan === 'learner')  return 'Learner';
  if (plan === 'proplus')  return 'Pro Plus';
  return 'Free';
}
```

---

### CHUNK 5 — Plan Badge (navbar avatar area)
**Files touched:** `navbar.component.html`, `navbar.component.scss`
**Risk:** Low — purely additive visual element

Show a small pill badge **beside the avatar** in the desktop logged-in nav (not for admin):
```
[🔵 Learner]  [avatar]
[🟡 Pro Plus ⚡] [avatar]
(nothing shown for Free plan)
```

#### Badge color scheme
| Plan | Color | Style |
|------|-------|-------|
| Free | — | No badge shown |
| Learner | `#3291b9` (brand blue) | Solid pill |
| Pro Plus | `#f5a623` amber/gold | Gradient pill with ⚡ |

Badge animates in with a subtle `fade-in + slide` on load (CSS only).
Also shown in the mobile menu header area beside the user name.

---

### CHUNK 6 — Stripe Success Redirect Handler
**Files created:** `src/app/core/services/plan-redirect.service.ts`
**Files touched:** `app.ts` (inject service in constructor to activate it on boot)
**Risk:** Low — read-only query param listener, no routing changes

#### How it works
1. You configure Stripe Payment Link success URL as:
   `https://yourapp.com/?plan=learner` (or `proplus`)
2. On every app boot, `PlanRedirectService` checks `window.location.search` for `?plan=`
3. If found and user is logged in → calls `auth.setPlan(plan)` → clears param from URL
4. If user not logged in yet → stores in `sessionStorage` → picks up after login

#### Instructions for Stripe Dashboard config (to be done manually)
```
Learner Monthly  success URL → https://[your-domain]/?plan=learner
Learner Yearly   success URL → https://[your-domain]/?plan=learner
Pro Plus Monthly success URL → https://[your-domain]/?plan=proplus
Pro Plus Yearly  success URL → https://[your-domain]/?plan=proplus
```

---

## ✅ Acceptance Checklist

### Data Layer
- [ ] `User.plan` field exists (`'free' | 'learner' | 'proplus'`, optional)
- [ ] `AuthService.setPlan()` patches signal + localStorage

### Pricing Section
- [ ] 3 cards render with correct EGP prices, emoji icons, features
- [ ] Monthly/Yearly toggle updates prices + Stripe link targets reactively
- [ ] Free CTA → `/auth/register` via `routerLink`
- [ ] Paid CTAs → `<a target="_blank" rel="noopener">` with 4 clearly named Stripe constants
- [ ] Learner card visually elevated + "Most Popular" badge
- [ ] Yearly mode: strikethrough + discount badge visible
- [ ] Trust line visible below cards
- [ ] Fully responsive (3-col desktop → stacked mobile)
- [ ] Light + dark theme correct

### Navbar
- [ ] Guest nav shows `Pricing` link (desktop + mobile) → scrolls to section
- [ ] Profile dropdown plan text is a clickable "→ View Plans" link
- [ ] Plan badge shows beside avatar for Learner (blue) and Pro Plus (gold)
- [ ] No badge shown for Free or Admin users

### Stripe Redirect
- [ ] `PlanRedirectService` reads `?plan=` on boot
- [ ] Plan persists across refresh via localStorage
- [ ] URL param cleaned after reading

### Build
- [ ] `npx tsc --noEmit` passes clean
- [ ] `npx ng build` passes clean
- [ ] `git diff --stat` shows ZERO backend files touched

---

## 🚀 Execution Order

Build one chunk at a time, in order. Do not start the next chunk until the current one compiles clean.

```
CHUNK 1 → CHUNK 2 → CHUNK 3 → CHUNK 4 → CHUNK 5 → CHUNK 6
```

After all 6 chunks: paste real Stripe links into the 4 constants, configure success URLs in Stripe Dashboard, smoke test the full flow.
