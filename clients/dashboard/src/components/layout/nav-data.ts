import {
  Activity,
  CreditCard,
  FolderOpen,
  FolderTree,
  HeartPulse,
  LayoutDashboard,
  MessageCircle,
  Package,
  Receipt,
  ScrollText,
  Settings,
  ShieldCheck,
  Tags,
  Ticket,
  Trash2,
  Users,
  UsersRound,
  Wallet,
  Wifi,
  Newspaper,
} from "lucide-react";
import { ALL_TRASH_PERMISSIONS } from "@/lib/trash-permissions";

export type NavSpec = {
    to: string;
    label: string;
    labelKey?: string;
    icon: React.ComponentType<{
        className?: string;
    }>;
  /**
   * Permission required to see this item. Items without a `perm` are visible to
   * every authenticated tenant user; gated items are hidden when the current
   * user (or impersonated user) lacks the permission, so they never land on a
   * page the API will reject with 403.
   */
  perm?: string;
  /**
   * Visible only if the user holds *at least one* of these permissions. Use for
   * an item that fronts several independently-gated sub-views (e.g. Trash, whose
   * tabs each require a different permission) — the entry should show as long as
   * the user can reach any one of them. Combined with `perm` via AND.
   */
  anyPerm?: readonly string[];
};

export type NavSection = {
    id: string;
    caption: string;
    captionKey?: string;
    icon: React.ComponentType<{
        className?: string;
    }>;
    items: NavSpec[];
};

// Top-level items live OUTSIDE any section. Overview opens the app;
// Settings is account-scoped and lives at the very bottom.
export const topNavTop: NavSpec[] = [
    {
        to: "/",
        label: "Overview",
        labelKey: "items.overview",
        icon: LayoutDashboard,
    },
    {
        to: "/chat",
        label: "Chat",
        labelKey: "items.chat",
        icon: MessageCircle,
        perm: "Permissions.Chat.Channels.View",
    },
    {
        to: "/files",
        label: "My Files",
        labelKey: "items.myFiles",
        icon: FolderOpen,
        perm: "Permissions.Files.Upload",
    },
];

export const topNavBottom: NavSpec[] = [
    {
        to: "/settings",
        label: "Settings",
        labelKey: "items.settings",
        icon: Settings,
    },
];
// Section accordion. Single-select — only one section open at a time.
export const sections: NavSection[] = [
  {
    id: "operations",
    caption: "Operations",
    captionKey:"sections.operations",
    icon: Activity,
    items: [
      // Live activity is SSE-backed; the stream is auth-only (no permission), so no gate.
        {
            to: "/activity",
            label: "Live activity",
            labelKey: "items.liveActivity",
            icon: Activity
        },
        {
            to: "/subscription",
            label: "Subscription",
            labelKey: "items.subscription",
            icon: CreditCard,
            perm: "Permissions.Billing.View"
        },
        {
            to: "/wallet",
            label: "WhatsApp wallet",
            labelKey: "items.whatsAppWallet",
            icon: Wallet,
            perm: "Permissions.Billing.View"
        },
        {
            to: "/invoices",
            label: "Invoices",
            labelKey: "items.invoices",
            icon: Receipt,
            perm: "Permissions.Billing.View"
        },
    ],
  },
{
    id: "market-intelligence",
    caption: "Market Intelligence",
    captionKey: "sections.marketIntelligence",
    icon: Newspaper,
    items: [
        {
            to: "/market-intelligence/disclosures",
            label: "Disclosures",
            labelKey: "items.disclosures",
            icon: Newspaper,
            perm: "Permissions.MarketIntelligence.Disclosures.View",
        },
  ],
},
  {
    id: "catalog",
    caption: "Catalog",
    captionKey : "sections.catalog",
    icon: Package,
    items: [
        {
            to: "/catalog/products",
            label: "Products",
            labelKey: "items.products",
            icon: Package,
            perm: "Permissions.Catalog.Products.View"
        },
        {
            to: "/catalog/brands",
            label: "Brands",
            labelKey: "items.brands",
            icon: Tags,
            perm: "Permissions.Catalog.Brands.View"
        },
        {
            to: "/catalog/categories",
            label: "Categories",
            labelKey: "items.categories",
            icon: FolderTree,
            perm: "Permissions.Catalog.Categories.View"
        },
    ],
  },
  {
    id: "helpdesk",
    caption: "Helpdesk",
    captionKey:"sections.helpdesk",
    icon: Ticket,
    items: [
        {
            to: "/tickets",
            label: "Tickets",
            labelKey:"items.tickets",
            icon: Ticket,
            perm: "Permissions.Tickets.View"
        },
    ],
  },
  {
    id: "identity",
    caption: "Identity",
    captionKey:"sections.identity",
    icon: Users,
    items: [
      // Gate the identity-management pages on a manage permission (not View): View Users/Roles/Groups
      // are IsBasic so every member holds them (the chat/user picker relies on Users.View), but only
      // managers should see these admin pages. Basic lacks the *.Update perms, so the items hide for them.
        {
            to: "/identity/users",
            label: "Users",
            labelKey: "items.users",
            icon: Users,
            perm: "Permissions.Users.Update"
        },
        {
            to: "/identity/roles",
            label: "Roles",
            labelKey: "items.roles",
            icon: ShieldCheck,
            perm: "Permissions.Roles.Update"
        },
        {
            to: "/identity/groups",
            label: "Groups",
            labelKey: "items.groups",
            icon: UsersRound,
            perm: "Permissions.Groups.Update"
        },
    ],
  },
  {
    id: "system",
    caption: "System",
    captionKey:"sections.system",
    icon: HeartPulse,
    items: [
      // Health hits the anonymous /health/ready probe — visible to everyone.
        {
            to: "/system/health",
            label: "Health",
            labelKey:"items.health",
            icon: HeartPulse
        },
        {
            to: "/system/audits",
            label: "Audit trail",
            labelKey:"items.auditTrail",
            icon: ScrollText,
            perm: "Permissions.AuditTrails.View"
        },
        {
            to: "/system/sessions",
            label: "Sessions",
            labelKey:"items.sessions",
            icon: Wifi,
            perm: "Permissions.Sessions.ViewAll"
        },
      // Trash fronts five tabs, each gated on a different resource's restore /
      // view-trash permission. Show the entry if the user can reach any tab; the
      // page hides the individual tabs they can't (see trash-permissions.ts).
        {
            to: "/system/trash",
            label: "Trash",
            labelKey:"items.trash",
            icon: Trash2,
            anyPerm: ALL_TRASH_PERMISSIONS
        },
    ],
  },
];

/** True when the user satisfies the item's gates: the single `perm` (if any)
 *  AND at least one of `anyPerm` (if any). Ungated items are always visible. */
function isNavItemVisible(item: NavSpec, permissions: readonly string[]): boolean {
  if (item.perm && !permissions.includes(item.perm)) return false;
  if (item.anyPerm && !item.anyPerm.some((p) => permissions.includes(p))) return false;
  return true;
}

/** Drop items the user can't access, then drop any section left empty. */
export function visibleSections(permissions: readonly string[]): NavSection[] {
  return sections
    .map((s) => ({ ...s, items: s.items.filter((i) => isNavItemVisible(i, permissions)) }))
    .filter((s) => s.items.length > 0);
}

/** Filter a flat nav list (top/bottom) by permission. */
export function visibleItems(items: NavSpec[], permissions: readonly string[]): NavSpec[] {
  return items.filter((i) => isNavItemVisible(i, permissions));
}

/** Find the section whose items contain the given path (best prefix match). */
export function findSectionForPath(pathname: string): string | null {
  let bestId: string | null = null;
  let bestLen = 0;
  for (const s of sections) {
    for (const item of s.items) {
      if (
        (item.to === "/" && pathname === "/") ||
        (item.to !== "/" && pathname.startsWith(item.to))
      ) {
        if (item.to.length > bestLen) {
          bestLen = item.to.length;
          bestId = s.id;
        }
      }
    }
  }
  return bestId;
}
