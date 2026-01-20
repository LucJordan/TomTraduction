# TomTraduction

Application de gestion de traductions développée avec ASP.NET Core Razor Pages et .NET 9.

## Fonctionnalités

- Recherche et filtrage de traductions
- Support multilingue (Français, Anglais, Portugais)
- Interface web responsive

## Technologies utilisées

- .NET 9
- ASP.NET Core Razor Pages
- Entity Framework (si applicable)
- Bootstrap (si applicable)

## Installation

1. Cloner le repository
2. Ouvrir avec Visual Studio 2022
3. Restaurer les packages NuGet
4. Lancer l'application

## Configuration

### Chemin des ressources de traduction

Le chemin des ressources de traduction est configuré dans le fichier `appsettings.json` :

````````json
{
  "Translation": {
    "ResourcesBasePath": "chemin/vers/vos/ressources"
  }
}
````````

**Important :** Vous devez modifier la valeur de `ResourcesBasePath` pour correspondre au chemin de votre environnement local où se trouvent les fichiers de ressources de traduction.

### Installation et configuration

1. Clonez le projet
2. Ouvrez le fichier `appsettings.json`
3. Modifiez la propriété `Translation.ResourcesBasePath` pour pointer vers le répertoire contenant vos ressources de traduction
4. Lancez l'application