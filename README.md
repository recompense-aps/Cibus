# Cibus

I really hate finding recipes on the internet. Everytime I load a recipe website
I have to navigate through ad after ad and close pop ups and scroll through
stories and videos. I just want to cook. No more bs, just recipes.

Cibus is a web app for stripping out the fluff of online recipes. It allows the
user to get and optionally save a recipe via url with the extra garbage stripped out.

## Roadmap
As of now, cibus is not meant to be a fully operationl app, but rather a proof of concept.

It would be cool to have this as an open source project with anyone contributing parsers,
but thats pretty far fetched.

Recipe sites that intended for support: (the goal is 5)
- [ ] [All Recipes](https://www.allrecipes.com/)
- [ ] [Simply Recipes](https://www.simplyrecipes.com/)
- [ ] [Tasty](https://tasty.co/)
- [ ] [Taste of Home](https://www.tasteofhome.com/)
- [ ] [Food network](https://www.foodnetwork.com/)
- [ ] Some sort of generic parser as well (not intended to be fool proof)
- [ ] In-app searchability of these websites

The user might want to save recipes as well
- [ ] Functional user accounts

## Development
Cibus is a .NET Core app using c# on the backend as well as the frontend via [Blazor](https://dotnet.microsoft.com/en-us/apps/aspnet/web-apps/blazor).
Cibus uses [Entity Framkework Core](https://docs.microsoft.com/en-us/ef/core/) on top of a SqlLite database.

### Backend
The backend is currently set up as a console app. It contains all the core data sctructures
as well as database operations and any other core logic. The console provides a set of commands
for interacting with the database. See the docs for how to use it.

To modify the database structure:
-	In `Cibus.Backend`:
-	Make modifications to `Model.cs`
-	run command `dotnet ef migrations add NameOfYourMigration`
-	run command `dotnet ef database update`

### Frontend
Blazor! No javascript.

### Tests
Cibus uses xUnit for unit testing and can be found in `Cibus.Tests`

`dotnet test` will execute the unit tests.