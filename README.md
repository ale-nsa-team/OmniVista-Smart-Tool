# **OmniVista Smart Tool**

## Disclaimer
© 2024 ALE USA Inc. All Rights Reserved. Permission to use, copy, modify, and distribute this source code and its documentation without a fee and without a signed license agreement is hereby granted, provided that the copyright notice, this paragraph, and the following two paragraphs appear in all copies, modifications, and distributions.
 
IN NO EVENT SHALL ALE USA INC. BE LIABLE TO ANY PARTY FOR DIRECT, INDIRECT, SPECIAL, INCIDENTAL, OR CONSEQUENTIAL DAMAGES, INCLUDING LOST PROFITS, ARISING OUT OF THE USE OF THIS SOURCE CODE AND ITS DOCUMENTATION, EVEN IF ALE USA INC. HAS BEEN ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 
ALE USA INC. SPECIFICALLY DISCLAIMS ANY WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE. THE SOURCE CODE AND ACCOMPANYING DOCUMENTATION, IF ANY, PROVIDED HEREUNDER IS PROVIDED “AS IS.” ALE USA INC. HAS NO OBLIGATION TO PROVICE MAINTENANCE, SUPPORT, UPDATES, ENHANCEMENTS, OR MODIFICATIONS.

## Description
The **OmniVista Smart Tool (OST)** is a free, field-ready companion app designed to simplify the deployment, configuration, and troubleshooting of **ALE switches** in OT environments.  
Unlike cloud-managed platforms, OST delivers a **standalone, on-site utility** that empowers entry-level technicians to install and maintain **ALE switches** in under an hour — reducing support load, accelerating partner onboarding, and improving customer retention.

## Distribution  

OST is distributed as a single installer package:  

`OmniVista-SmartTool-Setup-vx.x.x.msi`  

Users simply install the MSI and start the application.  

## Key Features  

- 🚀 **Simple Setup** – Wizard-based configuration, no CLI, no console cables, no prior training required  
- 🔌 **PoE Diagnostics** – Verify every device, resolve common PoE issues, check budgets, automate TAC troubleshooting  
- 📡 **Cable & Device Tools** – Built-in TDR for cable health, per-port LLDP device and power data  
- 🛠 **Field-Ready Utilities** – One-click repair, outdated software warnings, auto-save of all changes  
- 💾 **Backup & Restore** – Easy one-time switch backup/restore for RMA swaps or emergencies  
- 🔒 **Secure & Expandable** – Customizable without modifying AOS  

## Why OmniVista Smart Tool?  

- Reduces dependency on CLI  
- Trains new installers quickly  
- Minimizes downtime in high-turnover environments  
- Provides **rapid, reliable switch configuration on any ALE switch**  

## Configuration & Logs  

User data is stored under `%AppData%\Alcatel-Lucent Enterprise\AOS Toolkit`:  

- `Log\` → log files  
- `app.cfg` → user configuration (theme, language, saved switches, password hash)  

⚠️ The first-time password is hardcoded in `Data/Constants.cs (DEFAULT_PASS_CODE)`.  
Once changed, the built-in password is no longer valid. To reset, delete `app.cfg`.
