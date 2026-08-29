# Source register and research method

> **ARCHIVED SUPPORTING EVIDENCE — NON-AUTHORITATIVE.** Source snapshot and
> method with a 2026-08-29 cutoff. See the [archive index](README.md).

Research cutoff: **2026-08-29**. Source access dates are 2026-08-29 unless a row says otherwise.

## Evidence rules

- **FACT — OFFICIAL CLAIM:** the vendor, platform owner, official support centre, verified engineering site, repository, privacy notice, pricing page or store listing states it. It is not independent verification.
- **FACT — USER-REPORTED EXPERIENCE:** a public review/community post states it. It is evidence that the experience occurred for that reporter, not market prevalence or root cause.
- **INFERENCE:** a conclusion drawn across sources or from HomePlatform's repository.
- **RECOMMENDATION:** the proposed HomePlatform response.
- `UNKNOWN` means the targeted evidence search did not establish the claim. Absence of public documentation is not evidence that a feature/control is absent.
- Prices are snapshots, often locale-specific and mutable. App-store SKUs were not normalized when mapping was ambiguous.
- Pain frequency is a failure-mode recurrence signal: `HIGH` means at least three independent dated reports or an official multi-incident log; `MEDIUM` means at least two; `LOW` means one strong report. Ranges such as `MEDIUM-HIGH` reflect borderline count, age or source independence. None is a population prevalence estimate.

Source priority was: official product/support/security/pricing; official engineering/API/repositories/jobs; app-store product disclosures; dated store reviews and community reports. Third-party stack aggregators and unsourced comparison blogs were excluded from architecture claims.

## Discovery inventory — 46 products

The inventory is deliberately broader than products calling themselves a “family organizer.” Deep-dive status is shown for the 15 products analyzed in `COMPETITOR-PROFILES.md`.

| # | Product | Classification | Status at cutoff | Why relevant | Primary official source | Deep |
|---:|---|---|---|---|---|---|
| 1 | Cozi | Direct competitor | Active | shared calendar, lists, tasks, meals | [Cozi](https://www.cozi.com/) | yes |
| 2 | FamilyWall | Direct competitor | Active | multi-circle family suite, calendar, lists, meals, budget, location | [FamilyWall](https://www.familywall.com/index.html) | yes |
| 3 | Family Tools | Direct competitor | Active | roles, child profiles, chores, calendar, lists, rewards | [Family Tools](https://familytoolsapp.com/) | yes |
| 4 | Jam Family Calendar | Direct competitor | Active | family calendar, assistant, intake, lists and tasks | [Jam](https://www.jamfamilycalendar.com/) | yes |
| 5 | FamCal | Direct competitor | Active | shared calendar, tasks, recipes and travel expenses | [FamCal](https://www.famcal.app/home) | no |
| 6 | FamilyCal | Direct competitor | Active | calendar, tasks, shopping and wall-display mode | [FamilyCal](https://usefamilycal.com/) | no |
| 7 | Family Kiosk | Direct competitor | Active / new | bring-your-own wall display, chores, meals, lists and document AI | [Family Kiosk](https://kiosk.family/) | no |
| 8 | Ohai.ai | Direct competitor | Active | AI household assistant and schedule ingestion | [Ohai](https://www.ohai.ai/how-it-works/) | yes |
| 9 | DaCasa | Direct competitor | Active / new | calendar, chores, routines, caregivers and household money | [DaCasa](https://appdacasa.com/) | no |
| 10 | Maple | Direct competitor | Sunsetting 2026-12-31 | family OS, then exit/portability case | [Maple farewell](https://www.growmaple.com/farewell) | no |
| 11 | TimeTree | Adjacent competitor | Active | many shared calendars and calendar conversation | [TimeTree](https://timetreeapp.com/intl/en/) | yes |
| 12 | Any.do Family | Adjacent competitor | Active | family workspace layered on private tasks/calendar | [Any.do Family](https://www.any.do/en/family) | yes |
| 13 | Todoist | Adjacent competitor | Active | mature task collaboration, recurrence and sync reference | [Todoist collaboration](https://www.todoist.com/help/todoist/get-started/work-with-others-in-todoist-WOpFVjup7) | no |
| 14 | Life360 | Adjacent competitor | Active | family/circle location and safety coordination | [Life360](https://www.life360.com/en-us/plans-pricing) | no |
| 15 | Sweepy | Specialist | Active | cleaning schedule, assignments, child approval and points | [Sweepy](https://sweepy.com/) | yes |
| 16 | Tody | Specialist | Active | cleaning recurrence, rotations and completion history | [Tody](https://todyapp.com/) | no |
| 17 | Nipto | Specialist | Active | chore gamification, approval, rewards and leaderboard | [Nipto](https://nipto.app/) | no |
| 18 | Flatastic | Specialist | Active | shared-flat chores, shopping, expenses and pinboard | [Flatastic](https://www.flatastic-app.com/en/) | no |
| 19 | S'moresUp | Specialist | Active | chores, allowance, profiles, calendar and family networking | [S'moresUp](https://www.smoresup.app/) | no |
| 20 | Joon | Specialist | Active | task quests for neurodivergent children | [Joon](https://www.joonapp.io/) | no |
| 21 | BusyKid | Specialist | Active | chores, parent approval, allowance and child card | [BusyKid](https://busykid.com/busykid-features/) | no |
| 22 | Homey Chores & Allowance | Specialist | Active | responsibilities, paid jobs and allowance | [Homey](https://www.homeyapp.net/) | no |
| 23 | Bring! | Specialist | Active | real-time shopping lists, household sharing and recipes | [Bring!](https://www.getbring.com/en/features) | no |
| 24 | AnyList | Specialist | Active | real-time grocery lists, recipes and meal plan | [AnyList](https://www.anylist.com/complete) | yes |
| 25 | Paprika | Specialist | Active | recipes, meal planning and grocery lists | [Paprika](https://paprikaapp.com/) | no |
| 26 | Plan to Eat | Specialist | Active | recipe import, meal calendar and shopping list | [Plan to Eat](https://www.plantoeat.com/) | no |
| 27 | Mealime | Specialist | Active | preference-based meal planning and generated grocery lists | [Mealime](https://www.mealime.com/) | no |
| 28 | Meal Train | Specialist | Active | household support coordinated through external helpers | [Meal Train](https://www.mealtrain.com/) | no |
| 29 | CareCalendar.org | Specialist | Active | meals, rides, errands and housework coordinated by a network | [CareCalendar](https://www.carecalendar.org/) | no |
| 30 | Splitwise | Specialist | Active | household/group expenses, recurrence and balances | [Splitwise](https://secure.splitwise.com/) | yes |
| 31 | Dwellin | Specialist | Active | shared home maintenance, inventory and reminders | [Dwellin](https://www.dwellin.com/app/how-it-works) | yes |
| 32 | HomeZada | Specialist | Active | home inventory, maintenance, finances and multiple properties | [HomeZada](https://www.homezada.com/homeowners/pricing) | no |
| 33 | Centriq | Specialist | Active support surface; commercial status unverified | appliance inventory, manuals, recalls and maintenance | [Centriq help](https://help.poweredbycentriq.com/en/collections/2160541-getting-started) | no |
| 34 | OurFamilyWizard | Specialist | Active | co-parent calendar, expenses, records and restricted accounts | [OurFamilyWizard](https://www.ourfamilywizard.com/families) | yes |
| 35 | AppClose | Specialist | Active | co-parent circles, non-user flows, requests, records and payments | [AppClose](https://www.appclose.com/) | yes |
| 36 | TalkingParents | Specialist | Active | co-parent scheduling, documented communication and payments | [TalkingParents](https://talkingparents.com/features) | no |
| 37 | 2houses | Specialist | Active | co-parent calendar, expenses, records, to-dos and shopping | [2houses](https://www.2houses.com/) | no |
| 38 | CareCalendar Australia | Specialist | Active / new | two-home/blended-family roles, records, messages and expenses | [CareCalendar Australia](https://www.carecalendar.com.au/) | no |
| 39 | Skylight Calendar | Platform / wall display | Active | ambient calendar, chores, lists, meals and AI import | [Skylight features](https://skylight.zendesk.com/hc/en-us/articles/48778850390171-Calendar-Features) | yes |
| 40 | Hearth Display | Platform / wall display | Active | ambient family calendar, routines and meal plans | [Hearth features](https://hearthdisplay.com/pages/features) | no |
| 41 | DAKboard | Platform / wall display | Active | calendar integrations, chores and rewards | [DAKboard Family](https://dakboard.com/c/family/) | no |
| 42 | Mango Display | Platform / wall display | Active | calendar, tasks, chores, rewards and meal plan | [Mango pricing](https://mangodisplay.com/pricing/) | no |
| 43 | Greenlight Family Hub | Platform / wall display | Active / new | child profiles, parent PIN, chores, rewards and lists | [Greenlight Family Hub](https://help.greenlight.com/hc/en-us/articles/52138764283931-Greenlight-Family-Hub-Getting-started-guide) | no |
| 44 | Google Family | Platform / super-app | Active | family group, child accounts, calendar, notes and shopping | [Google Family overview](https://support.google.com/families/answer/15077335?hl=en) | yes |
| 45 | Apple Family Sharing | Platform / super-app | Active | individual and child accounts, roles, calendar and shared reminders | [Apple Family Sharing](https://support.apple.com/en-us/105062) | no |
| 46 | Microsoft 365 Family | Platform / super-app | Active | family group, calendar, Teams, OneNote and Copilot | [Microsoft 365 Family](https://support.microsoft.com/en-us/accounts-billing/subscriptions/microsoft-365-family) | no |

## Deep-dive official evidence register

| ID | Product | Evidence type | Source | Fact used |
|---|---|---|---|---|
| S001 | Cozi | pricing | [Compare plans](https://www.cozi.com/compare-plans/) | Free, Gold and Max pricing/features |
| S002 | Cozi | accounts/access | [FAQ](https://www.cozi.com/faq/) | up to 12; shared password; full access; removal by password change |
| S003 | Cozi | privacy | [Privacy policy](https://www.cozi.com/privacy-policy/) | child registration restriction and privacy rights |
| S005 | FamilyWall | features | [Product](https://www.familywall.com/index.html) | circles, calendar, tasks/lists, meals, budget, location, messages |
| S006 | FamilyWall | pricing | [Premium](https://www.familywall.com/premium.html?lang=en) | price and paywalled sync/location/meals/budget |
| S007 | FamilyWall | onboarding/identity | [Start your family](https://support.familywall.com/en/support/solutions/folders/47000698986) | create/join/invite, child without email, additional circle |
| S008 | FamilyWall | privacy | [Privacy](https://www.familywall.com/privacy.html) | location data, GDPR rights, security wording |
| S010 | Family Tools | pricing | [Premium](https://familytoolsapp.com/premium) | household price and premium capabilities |
| S011 | Family Tools | identity | [Account types](https://organizer.familytoolsapp.com/knowledge-base/account-types-on-family-tools/) | Parent, Standard and Linked accounts; loginless child path; Parent user management/removal |
| S012 | Family Tools | permissions | [Permissions](https://organizer.familytoolsapp.com/knowledge-base/permissions/) | role presets and per-user customization |
| S013 | Family Tools | recurrence | [Recurring chore](https://organizer.familytoolsapp.com/knowledge-base/how-to-add-a-reoccurring-chore-with-notifications/) | weekly/monthly repeat and multiple reminders |
| S015 | Jam | product/workflow | [How it works](https://www.jamfamilycalendar.com/how-it-works) | calendar/task/list intake and family flow |
| S016 | Jam | onboarding | [Invite family](https://help.jamfamilycalendar.com/article/19-how-to-invite-family-members) | invite flow |
| S017 | Jam | AI | [Family Assistant](https://help.jamfamilycalendar.com/article/42-jam-family-assistant) | assistant capabilities |
| S019 | TimeTree | free scope | [Free features](https://support.timetreeapp.com/hc/en-us/articles/360000290802-How-much-does-TimeTree-cost) | multiple calendars, comments/photos, imports |
| S020 | TimeTree | limits | [Calendar/member limits](https://support.timetreeapp.com/hc/en-us/articles/205924545-About-calendar-and-member-limit) | multi-calendar/member constraints |
| S021 | TimeTree | pricing | [Premium](https://support.timetreeapp.com/hc/en-us/articles/4647239978905-What-is-TimeTree-Premium) | per-account price and no shared subscription |
| S022 | TimeTree | security | [Security measures](https://support.timetreeapp.com/hc/en-us/articles/10404288026265-Security-Measures-Implemented-at-TimeTree) | governance, review and encryption claims |
| S023 | TimeTree | architecture | [Official 2020 stack](https://note.com/timetree_inc/n/n0fc57d42a92a) | historical client/backend/data/cloud stack |
| S024 | TimeTree | architecture | [2026 migration](https://note.com/timetree_inc/n/nd6b6451cfdb5?hl=en) | DynamoDB to Spanner and full-text search direction |
| S025 | TimeTree | reliability | [Official bug register](https://support.timetreeapp.com/hc/en-us/articles/360000329822-Bug-Report) | multi-incident 2025–26 sync/widget/notification failures |
| S026 | Any.do | pricing | [Pricing](https://www.any.do/pricing/) | Family price, members/projects and plan comparison |
| S027 | Any.do | onboarding/sharing | [Family workspace setup](https://support.any.do/en/articles/8610805-how-to-create-and-set-up-a-shared-space-in-any-do-family-workspace) | shared space and invitation model |
| S028 | Any.do | security | [Security and privacy](https://support.any.do/en/articles/9608389-account-security-and-privacy-in-any-do) | transit encryption, new-session code, copy/deletion |
| S030 | Ohai | product/workflow | [How it works](https://www.ohai.ai/how-it-works/) | AI ingestion, schedule and household assistant claims |
| S032 | Sweepy | product | [Sweepy](https://sweepy.com/) | cleaning schedule, household assignments, approval and points |
| S033 | Sweepy | store disclosure | [Google Play](https://play.google.com/store/apps/details?hl=en_US&id=app.sweepy.sweepy) | feature/privacy disclosure and current review corpus |
| S034 | Skylight | product | [Calendar features](https://skylight.zendesk.com/hc/en-us/articles/48778850390171-Calendar-Features) | chores, lists, meals, profiles and devices |
| S035 | Skylight | AI | [Sidekick](https://skylight.zendesk.com/hc/en-us/articles/39335273393947-Skylight-Sidekick) | AI event/document import |
| S036 | Skylight | pricing | [Shop](https://shop.myskylight.com/) | hardware tiers and Plus entitlement |
| S037 | Skylight | privacy | [Privacy](https://uk.myskylight.com/privacy-policy/) | GDPR rights and encrypted cloud-media protocol claim |
| S039 | OurFamilyWizard | pricing | [Plans](https://www.ourfamilywizard.com/plans-and-pricing) | per-parent plan prices and free restricted accounts |
| S040 | OurFamilyWizard | identity/access | [Add child/third party](https://support.ourfamilywizard.com/hc/en-us/articles/26334084178061-How-do-I-add-or-remove-a-child-or-third-party-account) | child/third-party account with optional email/phone and restricted access; credentialless login not established |
| S041 | OurFamilyWizard | privacy/security | [Privacy](https://www.ourfamilywizard.com/legal/privacy) | encryption/location/erasure claims and shared-record exceptions |
| S042 | AppClose | product/pricing | [Product](https://www.appclose.com/) | circles, calendar, non-user workflows, price/trial and 2FA claim |
| S043 | AppClose | workflow/security | [Official brochure](https://appclose.com/info/AppClose-Brochure-PDF.pdf) | Solo, third parties, records, export and security claims |
| S044 | AppClose | privacy | [Privacy policy](https://www.appclose.com/privacy) | data categories, location, encryption and deletion semantics |
| S045 | AnyList | product/pricing | [AnyList Complete](https://www.anylist.com/complete) | shared lists, recipes, meal plan and household entitlement |
| S046 | AnyList | sharing | [Share recipes/meal plan](https://help.anylist.com/articles/share-recipes-meal-plan/) | household sharing behavior |
| S047 | AnyList | product | [Product](https://www.anylist.com/) | list sync and core capabilities |
| S048 | Splitwise | product | [Splitwise](https://secure.splitwise.com/) | groups, balances and expenses |
| S049 | Splitwise | recurrence | [Recurring expenses](https://kb.splitwise.com/balances-and-expenses/how-can-i-manage-recurring-expenses) | schedule and recurrence history |
| S050 | Splitwise | pricing | [Pro](https://kb.splitwise.com/pro/what-is-splitwise-pro) | four-expense daily free limit and paid capabilities |
| S051 | Splitwise | privacy/export | [Privacy](https://www.splitwise.com/privacy) | shared-record edit/delete and account-deletion semantics |
| S052 | Dwellin | product | [How it works](https://www.dwellin.com/app/how-it-works) | home inventory, maintenance, collaboration and reminders |
| S053 | Dwellin | product/limits | [FAQ](https://dwellin.com/faqs) | accounts, sharing and commercial details |
| S054 | Google | identity | [Family group](https://support.google.com/families/answer/7103337) | manager/member/child model, max six and one family group |
| S055 | Google | calendar | [Family calendar](https://support.google.com/families/answer/7157782) | automatic calendar, ownership and editing |
| S056 | Maple | lifecycle | [Shutdown notice](https://www.growmaple.com/farewell) | acquired, sunset date, export/transfer and deletion |
| S057 | Nipto | product | [Nipto](https://nipto.app/) | chores, leaderboard, rewards, approval and scheduling |
| S058 | Tody | pricing/product | [FAQ](https://todyapp.com/faq) | solo/household tiers and multi-home/sync capabilities |
| S059 | Hearth | hardware/pricing/privacy | [Display](https://hearthdisplay.com/products/hearth-display) | hardware and membership snapshot |
| S060 | Bring! | product/privacy | [Features](https://www.getbring.com/en/features) | real-time shared lists, notifications and shopping workflow |
| S061 | Todoist | pricing | [Pricing](https://www.todoist.com/pricing/) | plans and lack of family entitlement |
| S062 | Todoist | sync architecture | [API](https://developer.todoist.com/api/v1/) | incremental sync, optimistic updates, IDs and idempotency |
| S068 | Splitwise | technology | [Android engineering role](https://secure.splitwise.com/jobs/software_engineer_android) | first-party evidence for Kotlin Android work |
| S069 | Splitwise | technology | [Growth/server engineering role](https://secure.splitwise.com/jobs/software_engineer_growth) | Rails is valued in a current first-party server role; not proof of the whole backend |
| S070 | Bring! | privacy/technology | [Privacy policy](https://www.getbring.com/en/privacy-policy) | AWS hosting and named analytics/engagement integrations |
| S071 | Bring! | technology | [Careers](https://www.bringlabs.com/en/career) | first-party iOS engineering function |
| S072 | Todoist | security/privacy | [Privacy and security](https://www.todoist.com/help/todoist/get-started/todoist-privacy-and-security-LYvNRupva) | TLS, encryption at rest, optional 2FA, export/backups and deletion |
| S073 | OurFamilyWizard | account security | [Security settings](https://support.ourfamilywizard.com/hc/en-us/articles/26333955509389-How-do-I-update-my-security-settings) | password, security question, support code and app PIN; not evidence of MFA |
| S074 | Hearth | privacy disclosure | [Google Play](https://play.google.com/store/apps/details?id=com.hearth.hearthcompanion) | current store disclosure and review surface |
| S075 | Jam | pricing | [Subscription help](https://help.jamfamilycalendar.com/article/29-how-to-modify-or-cancel-your-jam-subscription) | current subscription amount/terms snapshot |
| S076 | Ohai | pricing/product | [How it works](https://www.ohai.ai/how-it-works/) | current Individual/Duo/Group plan snapshot and product claims |
| S078 | Tody | privacy | [Privacy policy](https://todyapp.com/privacy) | Firebase/Google services, retention and deletion claims |
| S085 | Google | family features | [Family service overview](https://support.google.com/families/answer/15077335?hl=en) | family calendar, shared notes and shopping-list capabilities |
| S086 | Hearth | privacy | [Privacy policy](https://hearthdisplay.com/pages/privacy-policy) | parent requests for child-profile data and privacy controls |
| S087 | Sweepy | pricing/store | [US App Store](https://apps.apple.com/us/app/sweepy-home-cleaning-schedule/id1498897320) | current price/trial snapshot and store disclosure |
| S088 | FamilyWall | identity/roles | [Child account without email](https://support.familywall.com/en/support/solutions/articles/47001239550-invite-a-member-without-email-child-account-) | no-email child still receives login/password; Founder/Administrator/member roles |
| S089 | Jam | notification channel | [Manage notifications](https://help.jamfamilycalendar.com/article/36-how-to-enable-and-manage-notifications-in-jam) | push-only notification delivery; no email notifications |
| S090 | Splitwise | permissions | [Group permissions](https://kb.splitwise.com/groups/can-i-prevent-group-members-from-changing-expenses-or-set-permissions) | no group admins, special permissions or creator authority |
| S091 | Dwellin | identity/roles | [Terms](https://dwellin.com/terms) | Primary/Sub-Account Holder model and adult account-holder requirement |
| S092 | TimeTree | ownership/revocation | [Creator and admin](https://support.timetreeapp.com/hc/en-us/articles/115000038561-About-the-creator-admin) | Creator member-removal authority, equal other permissions and succession |
| S093 | TimeTree | lifecycle | [Leave or delete shared calendars](https://support.timetreeapp.com/hc/en-us/articles/204698785-How-to-leave-or-delete-shared-calendars) | member leave, Creator succession and deletion after all members leave |
| S094 | Any.do | communication | [Activity, chat and mentions](https://support.any.do/en/articles/8613917-activity-chat-mentions-boards-tasks) | board/task-scoped chat, comments, mentions and activity |
| S095 | AnyList | notification channel | [Lists](https://www.anylist.com/lists) | optional push notifications for shared-list changes |
| S096 | Skylight | notification channel | [Task due reminders](https://skylight.zendesk.com/hc/en-us/articles/52390654789659--Feature-Task-Due-Reminders) | task due reminder delivery behavior |
| S116 | Life360 | privacy | [Privacy](https://www.life360.com/privacy_policy/) | location as a high-sensitivity product area |

## User-experience evidence register

These are `USER-REPORTED EXPERIENCE` unless marked “official bug register.” The matrix records a failure-mode recurrence signal, never a market share or prevalence estimate.

| ID | Products | Source set | Use |
|---|---|---|---|
| S004 | Cozi | [Google Play DE](https://play.google.com/store/apps/details?hl=de&id=com.cozi.androidfree), [Google Play PT](https://play.google.com/store/apps/details?hl=pt_PT&id=com.cozi.androidfree), [Reddit](https://www.reddit.com/r/apps/comments/1s9ipcj/digital_family_calendar_that_actually_syncs/) | state loss, history paywall and manual entry |
| S009 | FamilyWall | [App Store reviews](https://apps.apple.com/us/app/familywall-family-organizer/id496889629?see-all=reviews) | save/reset/widget/location/notification reports |
| S014 | Family Tools | [Google Play](https://play.google.com/store/apps/details?id=app.familytools.twa&listing=chores), [App Store](https://apps.apple.com/gb/app/family-tools-family-organizer/id1541899076) | notification, calendar, locale-date and freeze reports |
| S029 | Any.do | [App Store reviews](https://apps.apple.com/us/app/any-do-to-do-list-planner/id497328576?platform=watch&see-all=reviews) | cross-device sync and lost reminders/subtasks |
| S038 | Skylight | [sync](https://www.reddit.com/r/skylightcalendar/comments/1ud427t/calendar_sync_issues_again/), [timezone/edit](https://www.reddit.com/r/skylightcalendar/comments/1sl7mem/issues_with_calendar_syncing_and_editing_and/), [person mapping](https://www.reddit.com/r/skylightcalendar/comments/1vpwuk3/whats_the_point_if_calendars_dont_sync_by_person/) | repeated sync and identity-mapping reports |
| S063 | Bring! | [ES-MX Play](https://play.google.com/store/apps/details?hl=es_MX&id=ch.publisheria.bring), [NL Play](https://play.google.com/store/apps/details?hl=nl&id=ch.publisheria.bring) | disappearing shared-list items |
| S064 | Splitwise | [EN Play](https://play.google.com/store/apps/details?id=com.Splitwise.SplitwiseMobile), [ES Play](https://play.google.com/store/apps/details?hl=es_US&id=com.Splitwise.SplitwiseMobile), [FR Play](https://play.google.com/store/apps/details?hl=fr&id=com.Splitwise.SplitwiseMobile) | daily-limit/paywall frustration |
| S065 | OurFamilyWizard | [Google Play](https://play.google.com/store/apps/details?hl=en_US&id=com.ourfamilywizard), [Trustpilot](https://www.trustpilot.com/review/www.ourfamilywizard.com) | login, ToneMeter and billing complaint samples |
| S066 | Todoist | [Google Play](https://play.google.com/store/apps/details?hl=en_US&id=com.todoist), [pricing thread](https://www.reddit.com/r/todoist/comments/1otieep/todoist_pricing_update_addressing_your_questions/) | offline/sync and repricing reports |
| S067 | Hearth | [Google Play](https://play.google.com/store/apps/details?id=com.hearth.hearthcompanion), [BBB](https://www.bbb.org/us/ny/brooklyn/profile/home-electronics/hearth-0121-87174183/complaints) | routine/sync/support reports |
| S081 | Tody | [Google Play FR](https://play.google.com/store/apps/details/?hl=fr&id=com.looploop.tody), [Google Play PT](https://play.google.com/store/apps/details?hl=pt_PT&id=com.looploop.tody) | dependency-chain and reminder-granularity reports |
| S082 | Nipto | [App Store](https://apps.apple.com/us/app/nipto-split-chores/id1504877473), [Google Play FR](https://play.google.com/store/apps/details?gl=fr&hl=fr&id=com.nipto.niptoapp) | recurrence, scoring and child-reward reports |
| S083 | OurFamilyWizard | [App Store reviews](https://apps.apple.com/us/app/ourfamilywizard-co-parent-app/id497405393?see-all=reviews) | offline report with developer response |
| S084 | Todoist | [Google Play](https://play.google.com/store/apps/details?hl=en_US&id=com.todoist), [calendar discussion](https://www.reddit.com/r/todoist/comments/1sb1tpe/support_for_multiple_calendars/) | offline/sync and calendar-integration reports |

## HomePlatform repository evidence

Repository snapshot: repository root, branch `main`, HEAD `a87a8176676a118a2a684f02d0a2ab7f74eef182`, inspected read-only on 2026-08-29.

Key evidence is cited in the architecture documents with repository-relative `path:line` references. The most consequential current facts are:

- required initial owner and in-memory membership: `backend/src/HomePlatform.Domain/Household/Household.cs:13-59`;
- required `UserId` on every member: `backend/src/HomePlatform.Domain/Household/HouseholdMember.cs:3-21`;
- authorization vocabulary: `backend/src/HomePlatform.Domain/Household/HouseholdRole.cs:3-8`;
- stale handler constructor and caller-supplied actor: `backend/src/HomePlatform.Application/Households/CreateHousehold/CreateHouseholdHandler.cs:15-35` and `CreateHouseholdCommand.cs:3-6`;
- empty EF model: `backend/src/HomePlatform.Infrastructure/Persistence/HomePlatformDbContext.cs:1-6`;
- API maps only OpenAPI/health: `backend/src/HomePlatform.Api/Program.cs:4-21`;
- current beta scope and exclusions: `docs/product/PRODUCT-SCOPE.md`;
- current role-versus-relationship intent: `docs/architecture/target/DOMAIN-MODEL.md`;
- current phase sequence: `docs/architecture/roadmap/HOMEPLATFORM-6-MONTH-MASTERPLAN.md`;
- architectural trigger table: `docs/architecture/target/TARGET-ARCHITECTURE.md`.

## Known limitations

- Public product pages do not prove actual runtime behavior, uptime, security controls or implementation quality.
- Review samples are self-selected, locale-dependent and lack a denominator.
- Prices, trials and free limits can change immediately after the cutoff.
- Hardware discounts were treated as temporary snapshots, not durable list prices.
- For competitor architecture, only TimeTree had deep first-party stack evidence. Todoist exposed sync semantics; Splitwise and Bring! exposed partial technology signals. Most core architectures remain `UNKNOWN`.
- The HomePlatform current build/tests were not executed; a deterministic constructor mismatch was established by source inspection. Production, cloud, mobile, frontend, load, offline and realtime behavior are `NOT IMPLEMENTED` or `NOT VERIFIED`.

## Artifact validation

`AUDIT-VALIDATION.ipynb` is the offline structural/data-quality gate. It checks the 46-product inventory, 15 deep dives, all 1,875 long-format feature rows, all 125 feature dimensions, pain rows, required files/sections/decisions, source-ID resolution and relative links.

The Markdown gate uses `markdownlint-cli2` with only `MD013` (line length) and `MD060` (table-column style) disabled because the evidence register intentionally contains long URLs and dense tables. All other enabled rules must pass. Equivalent config:

```json
{
  "config": {
    "MD013": false,
    "MD060": false
  }
}
```
