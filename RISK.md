# Account and usage risks

**Do not use this project if you cannot accept additional account risk. Use the official launcher without third-party Mods instead.**

## Account penalties

This software is unofficial and has no endorsement or permission from the game operator. Parent-process bridging changes the normal launch chain. XXMI / EFMI Mod loading involves third-party DLL injection. Cosmetic-only changes do not establish an exemption from game rules.

The mainland-China Endfield agreement restricts unauthorized third-party tools and certain runtime modifications in section 6.2; section 6.3 lists possible penalties, including temporary or permanent account-login restrictions. The official site has also published enforcement notices for unauthorized software and malicious client modification. These documents do not provide a project-specific approval or a measurable ban probability.

- [Official Endfield service agreement](https://user.hypergryph.com/protocol/plain/endfield/service)
- [Official enforcement notice](https://endfield.hypergryph.com/news/4787)

Rules and enforcement can change. Check the applicable region's current terms. We cannot tell whether your account is flagged. No successful test, community report, Windows signature or XXMI configuration signature guarantees safety. XXMI's Keep button authorizes a command locally; it does not grant permission from the game operator.

## Technical and privilege risks

- Launching may request administrator privileges. Run only builds and source you trust.
- The official helper remains suspended. Normal cleanup only terminates the helper created by this session; forcibly terminating the bridge may leave it behind.
- Paths, game updates, loader changes, or incompatible Mods may cause launch failures or crashes.
- Backup/restore handles local configuration, not any game-server history. It does not prevent or undo penalties.
- MIT licensing governs this source's copyright permissions. It does not authorize use against a game's terms.

## Data and distribution

The bridge has no account-password collection or log-upload feature and does not read XXMI private keys. This is a statement about the bridge's implementation, not a security audit of the game, loader, or installed Mods.

Local profiles, logs and backups may contain machine paths or third-party configuration. Do not upload them unreviewed. The public repository excludes runtime profiles, authorization keys, logs, backups, local test fixtures and compiled local outputs.

## Evidence boundaries

Automated tests exercise isolated fixtures and harmless self-process probes. A prior local user-run session produced an injection success log. Neither proves visual Mod correctness, official approval, undetectability, or absence of future penalties.
