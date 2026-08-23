import { Component, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';

// ─────────────────────────────────────────────────────────────────────────────
// ⬇️  REPLACE THESE WITH YOUR REAL STRIPE DASHBOARD LINKS BEFORE THE DEMO
// ─────────────────────────────────────────────────────────────────────────────
const STRIPE_LINK_LEARNER_MONTHLY  = 'https://buy.stripe.com/test_5kQ7sLgbcfKs2FM3AW2B200';
const STRIPE_LINK_LEARNER_YEARLY   = 'https://buy.stripe.com/test_REPLACE_ME_LEARNER_YEARLY';
const STRIPE_LINK_PROPLUS_MONTHLY  = 'https://buy.stripe.com/test_fZu8wPe349m40xEb3o2B201';
const STRIPE_LINK_PROPLUS_YEARLY   = 'https://buy.stripe.com/test_REPLACE_ME_PROPLUS_YEARLY';
// ─────────────────────────────────────────────────────────────────────────────

export interface PlanFeature {
  label: string;
  icon: string;
  included: boolean;
}

export interface PricingPlan {
  id: 'free' | 'learner' | 'proplus';
  name: string;
  tagline: string;
  monthlyPriceEGP: number;
  yearlyPriceEGP: number;
  yearlyBadge: string;
  features: PlanFeature[];
  ctaLabel: string;
  ctaType: 'route' | 'stripe-link';
  ctaTarget: string;
  highlighted: boolean;
}

const PLANS: PricingPlan[] = [
  {
    id: 'free',
    name: 'Free',
    tagline: 'Practice like LeetCode. No AI, no limits on problems.',
    monthlyPriceEGP: 0,
    yearlyPriceEGP: 0,
    yearlyBadge: '',
    features: [
      { label: 'Unlimited problems',           icon: '∞',  included: true  },
      { label: 'Judge0-powered code execution', icon: '▶️', included: true  },
      { label: 'Submission history',            icon: '🕐', included: true  },
      { label: 'AI hints',                      icon: '✨', included: false },
      { label: 'Code quality analysis',         icon: '🛡️', included: false },
    ],
    ctaLabel: 'Get Started Free',
    ctaType: 'route',
    ctaTarget: '/auth/register',
    highlighted: false,
  },
  {
    id: 'learner',
    name: 'Learner',
    tagline: 'Guided practice with AI hints when you\'re stuck.',
    monthlyPriceEGP: 199,
    yearlyPriceEGP: 1990,
    yearlyBadge: 'Save ~17% · 2 months free',
    features: [
      { label: 'Everything in Free',               icon: '✅', included: true },
      { label: '10 AI hints / month',               icon: '✨', included: true },
      { label: 'Hints on up to 5 problems / month', icon: '🎯', included: true },
      { label: 'Basic code quality feedback',       icon: '🛡️', included: true },
    ],
    ctaLabel: 'Choose Learner',
    ctaType: 'stripe-link',
    ctaTarget: STRIPE_LINK_LEARNER_MONTHLY,
    highlighted: true,
  },
  {
    id: 'proplus',
    name: 'Pro Plus',
    tagline: 'Full AI power for serious competitive programmers.',
    monthlyPriceEGP: 499,
    yearlyPriceEGP: 4990,
    yearlyBadge: 'Save ~17% · 2 months free',
    features: [
      { label: 'Everything in Learner',                         icon: '✅', included: true },
      { label: '300 AI hints / month',                          icon: '⚡', included: true },
      { label: 'Unlimited problems using hints',                icon: '∞',  included: true },
      { label: 'Full code quality + integrity analysis',        icon: '🛡️', included: true },
      { label: 'Priority support',                              icon: '💬', included: true },
    ],
    ctaLabel: 'Go Pro Plus',
    ctaType: 'stripe-link',
    ctaTarget: STRIPE_LINK_PROPLUS_MONTHLY,
    highlighted: false,
  },
];

@Component({
  selector: 'app-pricing',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './pricing.component.html',
  styleUrl: './pricing.component.scss',
})
export class PricingComponent {
  readonly billingCycle = signal<'monthly' | 'yearly'>('monthly');

  readonly plans = computed<PricingPlan[]>(() => {
    const cycle = this.billingCycle();
    return PLANS.map(plan => ({
      ...plan,
      ctaTarget: this.resolveCtaTarget(plan.id, cycle),
    }));
  });

  readonly isYearly = computed(() => this.billingCycle() === 'yearly');

  setBilling(cycle: 'monthly' | 'yearly'): void {
    this.billingCycle.set(cycle);
  }

  displayPrice(plan: PricingPlan): number {
    return this.isYearly() ? plan.yearlyPriceEGP : plan.monthlyPriceEGP;
  }

  strikethroughPrice(plan: PricingPlan): number {
    return plan.monthlyPriceEGP * 12;
  }

  private resolveCtaTarget(id: 'free' | 'learner' | 'proplus', cycle: 'monthly' | 'yearly'): string {
    if (id === 'free') return '/auth/register';
    if (id === 'learner') {
      return cycle === 'yearly' ? STRIPE_LINK_LEARNER_YEARLY : STRIPE_LINK_LEARNER_MONTHLY;
    }
    return cycle === 'yearly' ? STRIPE_LINK_PROPLUS_YEARLY : STRIPE_LINK_PROPLUS_MONTHLY;
  }
}
