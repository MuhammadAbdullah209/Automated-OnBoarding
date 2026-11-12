# Database migrations

Run these from the solution folder (the one that contains `AutomatedOnboarding.slnx`).

## Add a migration

After changing the model (an entity, a property, or the configuration in `OnboardingDbContext`):

```bash
dotnet ef migrations add YourMigrationName --project AOB.DAL --startup-project AOB.WEB
```

If the app is running and locks the Debug build ("file is being used by another process"), build in Release instead:

```bash
dotnet ef migrations add YourMigrationName --project AOB.DAL --startup-project AOB.WEB --configuration Release
```

Use a short name that says what changed, for example `AddInformationRequests`.
Check the generated file in this folder before using it: it should contain only the change you meant to make.

## Apply migrations

The app applies pending migrations when it starts (`DbSeeder.InitializeAsync`). To apply them by hand:

```bash
dotnet ef database update --project AOB.DAL --startup-project AOB.WEB
```

## Undo the last migration

Only for a migration that has not been applied to a database yet:

```bash
dotnet ef migrations remove --project AOB.DAL --startup-project AOB.WEB
```

If it was already applied, add a new migration that reverses the change instead.

## Notes

- Never edit `OnboardingDbContextModelSnapshot.cs` by hand; it is rewritten by `migrations add` and `migrations remove`.
- Enums are stored as text (for example `Status`, `BuildType`), so adding an enum value needs no migration. Renaming one needs a migration that updates the stored text.
