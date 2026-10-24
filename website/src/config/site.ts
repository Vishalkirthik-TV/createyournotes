/**
 * Centralized site configuration and content.
 *
 * Every product claim here was verified against the WPF application source.
 * Do NOT add features that are not actually implemented (e.g. Markdown,
 * cloud sync, encryption, folders, tags, pinning, docking, or a default
 * hotkey) — the app does not have them.
 */

export interface NavItem {
  label: string;
  href: string;
}

export interface Feature {
  eyebrow: string;
  title: string;
  body: string;
  points: string[];
  /** Visual variant drives which mockup component renders beside the copy. */
  visual: 'rail' | 'list' | 'editor' | 'palette' | 'hotkey' | 'offline';
  align: 'left' | 'right';
}

export interface Benefit {
  title: string;
  body: string;
  icon: string;
}

export interface FaqItem {
  question: string;
  answer: string;
}

export const siteConfig = {
  name: 'CreateYourNotes',
  tagline: 'Your thoughts. Your space. Your notes.',
  description:
    'Discover CreateYourNotes, a Windows notes application designed to help you capture ideas, organize thoughts, and stay focused.',
  /** Canonical domain — update if the site is deployed elsewhere. */
  url: 'https://createyournotes.app',
  platform: 'Windows',
  requirement: 'Windows 10 / 11 and later',
} as const;

/**
 * Single source of truth for outbound links.
 * The project lives on GitHub under the renamed "createyournotes" repository.
 */
export const links = {
  github: 'https://github.com/Vishalkirthik-TV/createyournotes',
  issues: 'https://github.com/Vishalkirthik-TV/createyournotes/issues',
  releases: 'https://github.com/Vishalkirthik-TV/createyournotes/releases',
} as const;

/**
 * Download destination is configured in exactly one place.
 *
 * No installers are published yet, so this points at the GitHub releases
 * page — a real URL that will list installers the moment a release ships.
 * When you publish a release, swap `url` for the direct asset link, e.g.
 * `.../releases/download/v1.0.0/CreateYourNotes-Setup-1.0.0.exe`.
 */
export const download = {
  url: 'https://github.com/Vishalkirthik-TV/createyournotes/releases/latest',
  label: 'Download for Windows',
  shortLabel: 'Download',
  note: 'Free download · Windows 10 / 11 and later',
} as const;

export const nav: NavItem[] = [
  { label: 'Features', href: '#features' },
  { label: 'Screenshots', href: '#showcase' },
  { label: 'FAQ', href: '#faq' },
  { label: 'GitHub', href: links.github },
];

/**
 * Verified feature storytelling. Layout alternates via `align`.
 * (The edge rail has its own dedicated section — EdgeRailStory — so it is
 * not repeated here.)
 */
export const features: Feature[] = [
  {
    eyebrow: 'All notes, one list',
    title: 'Everything in a single searchable window',
    body: 'Open every note at once in the All Notes window. Filter between All, Active and Archived, and preview any note in full beside the list — no digging through folders, because there are none to manage.',
    points: [
      'Search across titles and bodies as you type',
      'Filter by All, Active or Archived',
      'Master–list layout with a full preview pane',
      'Relative timestamps so recency is obvious',
    ],
    visual: 'list',
    align: 'left',
  },
  {
    eyebrow: 'Just start typing',
    title: 'A calm, focused place to write',
    body: 'Each note is a clean, borderless canvas. Choose a handwriting style for your words, spell-check catches slips, and everything saves itself the moment you pause — so you can stay with the thought instead of the file menu.',
    points: [
      'Distraction-free, borderless note windows',
      'Handwriting fonts, including a bundled favourite',
      'Built-in spell-check',
      'Autosave with a live “Saved” indicator',
    ],
    visual: 'editor',
    align: 'right',
  },
  {
    eyebrow: 'Make it yours',
    title: 'Colours and type that feel like you',
    body: 'Pick from five built-in note colours or add your own with a hex value or the Windows colour picker — as many as you like. Switch the handwriting and interface fonts with a live preview so you can see the change before you commit.',
    points: [
      'Five built-in colours, plus unlimited custom ones',
      'Add colours by hex or the system colour picker',
      'Choose your handwriting and interface fonts',
      'Live preview of every change',
    ],
    visual: 'palette',
    align: 'left',
  },
  {
    eyebrow: 'Never lose the thread',
    title: 'A hotkey that opens everything',
    body: 'Assign a global shortcut that pulls open your whole note list from anywhere in Windows. Record any combination you like — or set it to None. It works no matter which app has focus, and you stay in control of the keys.',
    points: [
      'Global hotkey, open from any app',
      'Record any combination with a modifier',
      'Set to None to turn it off',
      'Lives quietly in the system tray',
    ],
    visual: 'hotkey',
    align: 'right',
  },
  {
    eyebrow: 'Yours, offline',
    title: 'Your notes stay on your PC',
    body: 'CreateYourNotes stores everything in a local database on your own machine. There is no account to create, nothing to sync, and no tracking — it simply works, online or off, and it is there when you open the app.',
    points: [
      'Stored locally on your computer',
      'No account, no sign-in, no server',
      'Works fully offline',
      'Export notes to .txt or .json any time',
    ],
    visual: 'offline',
    align: 'left',
  },
];

/** Everyday value, not technical detail. */
export const benefits: Benefit[] = [
  {
    title: 'Less friction, more thinking',
    body: 'Reach the edge, fan the deck, start typing. Capture a thought in the time it takes to open a browser tab.',
    icon: 'zap',
  },
  {
    title: 'Always where you left it',
    body: 'Notes keep their colour, their place and their recency — so the one you need is never buried.',
    icon: 'layout',
  },
  {
    title: 'Quiet by design',
    body: 'No accounts, no notifications, no upsells. A small, focused tool that stays out of your way.',
    icon: 'eye-off',
  },
  {
    title: 'Feels like your notebook',
    body: 'Your colours, your fonts, your shortcut. A few small choices make it unmistakably yours.',
    icon: 'palette',
  },
];

export const faq: FaqItem[] = [
  {
    question: 'What is CreateYourNotes?',
    answer:
      'CreateYourNotes is a lightweight sticky-notes app for Windows. Your notes rest as a slim stripe at the right edge of the screen, fan out when you reach for them, and open into a clean writing canvas when you need to think on the page.',
  },
  {
    question: 'Which operating systems are supported?',
    answer:
      'CreateYourNotes is a Windows desktop application, built for Windows 10 / 11 and later. It is not available for macOS, Linux, or the web.',
  },
  {
    question: 'How do I install it?',
    answer:
      'Grab the latest installer from the GitHub releases page and run it. The app is a standard Windows program — no command line or extra runtimes required.',
  },
  {
    question: 'Is CreateYourNotes free?',
    answer:
      'Pricing and licensing details are published alongside each release on GitHub. Downloading and trying the app is free — check the releases page for the most current information.',
  },
  {
    question: 'Where can I report a bug or request a feature?',
    answer:
      'Open an issue on the GitHub repository. Add a short description of what happened, what you expected, and your Windows version so it can be reproduced quickly.',
  },
  {
    question: 'Where can I find new releases?',
    answer:
      'New releases are posted on the GitHub releases page, where you can download the newest installer and read the changelog for each version.',
  },
];
