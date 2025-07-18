# dontloseit
A private flea market.

## New features

* Listings can now be reserved and marked as sold.
* Visitors can search across all active listings.
* Sellers can manage item photos when editing.

## Registration Password

The site requires a shared password for new accounts. Set the
`RegistrationPassword` value in *appsettings.json* to control who can join.

## External Authentication

The site supports Microsoft, Google, and Facebook logins. Configure the
credentials using environment variables:

```
Authentication__Microsoft__ClientId=<id>
Authentication__Microsoft__ClientSecret=<secret>
Authentication__Google__ClientId=<id>
Authentication__Google__ClientSecret=<secret>
Authentication__Facebook__AppId=<id>
Authentication__Facebook__AppSecret=<secret>
```

These values can also be stored in user secrets during development.


## Deployment

Use the `scripts/safe-deploy.ps1` script to deploy to IIS. The script stops the
application pool if required, copies the published files using `robocopy`, and
ensures the website is started when the deployment completes.
