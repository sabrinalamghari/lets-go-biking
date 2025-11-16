**Récap – Mise en place du service ProxyCacheService**

**Étapes réalisées :**
**Ajout d’un nouveau projet à la solution : ProxyCacheService**
→ Type : Application Console (.NET Framework 4.7.2)

**Ajout des références nécessaires :**
- System.ServiceModel (pour le WCF)
- System.Runtime.Caching (pour le cache mémoire)

**Création des 3 fichiers principaux :**
- IProxyService.cs → interface WCF (contrat du service)
- ProxyService.cs → implémentation avec HttpClient + MemoryCache
- Program.cs → point d’entrée avec configuration self-host du service

**Configuration du service :**
- Base address : http://localhost:9001/ProxyService
- Binding : BasicHttpBinding
- Cache TTL par défaut : 30 secondes
- Défini ProxyCacheService comme projet de démarrage

Lancement du serveur :
Console affiche :
``` 
ProxyCacheService started at http://localhost:9001/ProxyService
Press ENTER to stop...
```
Important : lancer Visual Studio en Administrateur (nécessaire pour enregistrer l’URL HTTP).
Le serveur WCF est auto-hébergé et accessible à l’adresse
http://localhost:9001/ProxyService
(prêt à être appelé depuis le futur RoutingService ou un client SOAP)



## Étape : Validation du ProxyCacheService (test de communication)

### Objectif

Vérifier que le service WCF `ProxyCacheService` fonctionne correctement et met en cache les réponses des appels HTTP externes.

---

### Configuration réalisée

* **Projet :** `ProxyCacheService` (.NET Framework 4.7.2)
* **Fichiers :**

  * `IProxyService.cs` → contrat WCF exposant la méthode `Get(string url)`
  * `ProxyService.cs` → implémentation du service avec `HttpClient` et `MemoryCache`
  * `Program.cs` → auto-hébergement du service via `ServiceHost`
* **Adresse de base du service :** `http://localhost:9001/ProxyService`
* **Durée de vie du cache (TTL) :** 30 secondes
* **Exécution :** Visual Studio en administrateur

---

### Test du service (ProxySmokeTest)

* Création d’un second projet : `ProxySmokeTest` (Application console .NET Framework)
* Référence ajoutée : `System.ServiceModel`
* Objectif : jouer le rôle du futur `RoutingService` pour tester la communication SOAP.

#### Code du client

```csharp
var binding = new BasicHttpBinding();
var endpoint = new EndpointAddress("http://localhost:9001/ProxyService");
var factory = new ChannelFactory<IProxyService>(binding, endpoint);
var proxy = factory.CreateChannel();

var url = "https://api.ipify.org?format=json";
Console.WriteLine(proxy.Get(url));
```

---

### Résultat attendu

#### Console **ProxyCacheService**

```
ProxyCacheService started at http://localhost:9001/ProxyService
Press ENTER to stop...
[Cache MISS] Fetching https://api.ipify.org?format=json
[Cache HIT] https://api.ipify.org?format=json
```

#### Console **ProxySmokeTest**

```
Requête 1...
{"ip":"46.xxx.xxx.xxx"}

Requête 2 (cache)...
{"ip":"46.xxx.xxx.xxx"}

Test terminé. Appuyez sur Entrée pour quitter.
```

✅ Le premier appel obtient la donnée depuis Internet (**MISS**)
✅ Le second appel renvoie la même donnée depuis le cache (**HIT**)

---

### Conclusion

* `ProxyCacheService` est **opérationnel** et auto-hébergé
* La **communication SOAP** entre deux projets fonctionne
* La **mise en cache MemoryCache** est fonctionnelle
* Le proxy est prêt à être intégré au `RoutingService`

---

### Prochaines étapes

1. Intégrer le proxy dans le `RoutingService` (remplacer `ProxySmokeTest`)
2. Utiliser un client SOAP (`ChannelFactory<IProxyService>`) dans le endpoint `/route`
3. Supprimer `ProxySmokeTest` une fois l’intégration validée


MemoryCache intégré dans ProxyService
Logs [Cache HIT]/[Cache MISS] fonctionnels
Tests réalisés via ProxySmokeTest (API ipify)
TTL = 30s

<html>
<body>
<!--StartFragment--><html><head></head><body>
<hr>
<h3>Ajouter la logique de cache dans ProxyService</h3>

Élément de l’issue | Implémenté ? | Où ça se trouve
-- | -- | --
Ajout du cache MemoryCache | ✅ | `ProxyService.cs` → `private readonly MemoryCache _cache = MemoryCache.Default;`
Stockage des réponses HTTP | ✅ | `_cache.Add(url, result, DateTimeOffset.Now.AddSeconds(30));`
Logs HIT/MISS | ✅ | `Console.WriteLine($"[Cache HIT] {url}");` et `Console.WriteLine($"[Cache MISS] Fetching {url}");`


<hr>

### IRoutingService.cs

Ce fichier définit l’interface du service WCF REST.
Il expose une seule méthode :
```bash
[WebGet(UriTemplate = "/route?from={from}&to={to}", ResponseFormat = WebMessageFormat.Json)]
RouteResult GetRoute(string from, string to);
```
Cette méthode est appelée depuis le navigateur pour calculer un itinéraire entre un point de départ (from) et un point d’arrivée (to).
Elle renvoie un objet RouteResult au format JSON, contenant les informations sur le trajet (distance totale, durée, étapes, mode utilisé, etc.).
C’est le “contrat” du service : tout client REST saura qu’il peut appeler /route?from=...&to=... pour obtenir un itinéraire.

### RoutingServiceImpl.cs

C’est la classe qui implémente réellement le service défini dans l’interface.
Elle contient la logique complète du calcul d’itinéraire :
Récupère les adresses (from, to) envoyées par le client.
Les convertit en coordonnées GPS via NominatimUtils.ParseOrGeocode.
Télécharge la liste des stations de vélos JCDecaux via JcDecauxClient.GetStations.
Trouve les stations les plus proches de l’origine et de la destination, avec vélos ou places disponibles.
Utilise OsrmClient pour calculer les trajets “à pied” et “à vélo”.
Compare les durées et distances afin de décider s’il vaut mieux marcher ou prendre un vélo JCDecaux.
Construit un objet RouteResult et le renvoie en JSON.
C’est le cœur du serveur de routage.

### RoutingHost/Program.cs

Ce fichier sert à héberger le service sans Visual Studio.
Il crée un serveur HTTP local grâce à WebServiceHost et démarre le service sur le port 9002 :
http://localhost:9002/

Il configure les bindings REST (JSON, HTTP GET), et affiche dans la console :

RoutingService REST démarré !
Test : http://localhost:9002/route?from=Paris&to=Lyon

Ce fichier permet de générer un exécutable .exe qu'on peut lancer depuis l’invite de commande, sans ouvrir Visual Studio.

### Models.cs
Ce fichier regroupe toutes les classes de données utilisées par le service :
LatLng → représente une position géographique (latitude / longitude).
RouteLeg → une “étape” du trajet (ex. marcher, pédaler…).
RouteResult → le résultat complet retourné au client (distance totale, durée, liste des étapes, mode “walk_only” ou “bike+walk”).
JcStation → une station JCDecaux (nom, position, nombre de vélos disponibles, nombre de places libres).
Toutes ces classes sont marquées avec [DataContract] et [DataMember] pour que WCF puisse les sérialiser en JSON.

### NominatimUtils.cs
Ce fichier contient un outil utilitaire de géocodage.
Son rôle :
Convertir une adresse textuelle ("Gare de Lyon, Paris") en coordonnées GPS (LatLng { lat=48.8443, lng=2.3730 }).
Inversement, si on reçoit directement des coordonnées, il les renvoie telles quelles.
Il utilise :
L’API Nominatim d’OpenStreetMap (gratuite, publique),
Il envoie les requêtes HTTP, lit le JSON renvoyé, et extrait les valeurs lat / lon.
C’est donc le pont entre des adresses humaines et des coordonnées GPS utilisables par OSRM.

### OsrmClient.cs
C’est le client pour le moteur d’itinéraire OSRM (Open Source Routing Machine).
Il sert à calculer :
des trajets à pied (RouteFoot),
des trajets à vélo (RouteBike).
Fonctionnement :
Reçoit deux positions GPS.
Appelle l’API OSRM (hébergée sur https://routing.openstreetmap.de/).
Récupère un JSON contenant la distance, la durée et les instructions.
Retourne un petit objet avec ces informations.

### JcDecauxClient.cs
C’est le client REST pour l’API JCDecaux.
Il permet d’obtenir la liste des stations de vélos et leurs disponibilités pour une ville (contrat JCDecaux).
Il :
appelle l’URL https://api.jcdecaux.com/vls/v3/stations?contract=Ville&apiKey=TaClé,
lit le JSON renvoyé,
extrait pour chaque station :
le nom,
la position,
le nombre de vélos disponibles,
le nombre de places libres.
C’est grâce à lui que le service sait où sont les stations, et combien de vélos sont disponibles.

### Constants.cs
Petit fichier utilitaire qui contient des valeurs globales réutilisées :
L’URL de base des APIs (JCDecaux, OSRM, etc.),
La clé JCDecaux (JCDECAUX_KEY)
Cela évite de répéter les mêmes constantes dans plusieurs fichiers et facilite la maintenance (une seule ligne à modifier si l’API change).

 Résumé global du fonctionnement
1- 	Program.cs	Démarre le serveur REST
2- 	IRoutingService.cs	Déclare l’API /route
3- 	RoutingServiceImpl.cs	Gère la logique du calcul de route
4- 	NominatimUtils.cs	Convertit les adresses en coordonnées
5- 	JcDecauxClient.cs	Récupère les stations JCDecaux
6- 	OsrmClient.cs	Calcule les distances et durées
7- 	Models.cs	Définit toutes les classes de données
8- 	Constants.cs	Contient les URLs et la clé API
</body></html><!--EndFragment-->
</body>
</html>


# Communication Proxy ↔ RoutingService (avec cache)

## 🎯 Objectif
Connecter le **RoutingService** au **ProxyCacheService** via SOAP afin que toutes les requêtes externes (ex : JCDecaux) passent par le proxy et bénéficient du cache `MemoryCache`.

---

## ⚙️ Implémentation réalisée

### 🔸 1. Ajout du client SOAP dans `RoutingServiceLib`

Création de la classe `Clients/ProxyClient.cs` :

```csharp
using System;
using System.ServiceModel;
using System.ServiceModel.Channels;

namespace RoutingServiceLib.Clients
{
    [ServiceContract]
    public interface IProxyService
    {
        [OperationContract]
        string Get(string url);
    }

    public class ProxyClient
    {
        private readonly string _endpointUrl;
        private readonly Binding _binding;
        private readonly EndpointAddress _endpoint;

        public ProxyClient(string endpointUrl = "http://localhost:9001/ProxyService")
        {
            _endpointUrl = endpointUrl;
            _binding = new BasicHttpBinding
            {
                MaxReceivedMessageSize = 10_000_000, // 10 Mo
                MaxBufferSize = 10_000_000,
                MaxBufferPoolSize = 10_000_000
            };
            _endpoint = new EndpointAddress(_endpointUrl);
        }

        public string Get(string url)
        {
            var factory = new ChannelFactory<IProxyService>(_binding, _endpoint);
            var ch = factory.CreateChannel();
            try
            {
                string res = ch.Get(url);
                ((IClientChannel)ch).Close();
                factory.Close();
                return res;
            }
            catch
            {
                ((IClientChannel)ch).Abort();
                factory.Abort();
                throw;
            }
        }
    }
}
````

---

### 🔸 2. Utilisation du Proxy dans `JcDecauxClient`

```csharp
using RoutingServiceLib.Clients;
using System.Web.Script.Serialization;

public class JcDecauxClient
{
    private static readonly ProxyClient _proxy = new ProxyClient("http://localhost:9001/ProxyService");

    public static List<JcStation> GetStations(string contract = "Lyon")
    {
        var list = new List<JcStation>();
        try
        {
            var url = $"{Constants.JCDECAUX}/stations?contract={Uri.EscapeDataString(contract)}&apiKey={Constants.JCDECAUX_KEY}";
            Console.WriteLine($"[JCDecaux] Fetching via Proxy: {url}");

            // ✅ Appel via ProxyCacheService
            var json = _proxy.Get(url);
            var rows = new JavaScriptSerializer().Deserialize<object[]>(json);
            Console.WriteLine($"[JCDecaux] raw stations = {rows?.Length ?? 0}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[JCDecaux] Error: {ex.Message}");
        }

        return list;
    }
}
```

---

### 🔸 3. Configuration du Proxy (`ProxyCacheService`)

Dans `Program.cs` :

```csharp
var binding = new BasicHttpBinding
{
    MaxReceivedMessageSize = 10_000_000,
    MaxBufferSize = 10_000_000,
    MaxBufferPoolSize = 10_000_000
};

host.AddServiceEndpoint(typeof(IProxyService), binding, "");
```

---

## 🧪 Tests effectués

### Lancement du ProxyCacheService (en administrateur)

```
ProxyCacheService started at http://localhost:9001/ProxyService
Press ENTER to stop...
```

### Lancement du RoutingHost

```
RoutingService REST démarré !
Test : http://localhost:9002/route?from=Paris&to=Lyon
```

### Appel du service REST

* Première requête → `[Cache MISS]`
* Deuxième requête (même URL, dans les 30s) → `[Cache HIT]`

**Console du Proxy :**

```
[Cache MISS] Fetching https://api.jcdecaux.com/vls/v3/stations?contract=Lyon&apiKey=...
[Cache HIT] https://api.jcdecaux.com/vls/v3/stations?contract=Lyon&apiKey=...
```

**Console du RoutingService :**

```
[Route] fetching JCDecaux stations for Lyon.
[JCDecaux] Fetching via Proxy: https://api.jcdecaux.com/vls/v3/stations?contract=Lyon...
[JCDecaux] raw stations = 350
[Route] stations fetched = 350
```

✅ **Communication Proxy ↔ Routing validée.**
Les appels REST passent bien par le proxy et bénéficient du cache `MemoryCache`.

---

## ⚠️ Problèmes rencontrés et solutions

| Problème                                                      | Cause                                                                                    | Solution                                                                                                                             |
| ------------------------------------------------------------- | ---------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------ |
| `Le quota de taille maximale autorisée (65536) a été dépassé` | Taille du message JCDecaux trop grande pour le binding par défaut                        | Augmentation de `MaxReceivedMessageSize`, `MaxBufferSize`, `MaxBufferPoolSize` à 10 Mo dans **ProxyCacheService** et **ProxyClient** |
| `[Cache HIT]` jamais visible lors des tests VS                | Le cache est perdu car Visual Studio relance le Proxy à chaque exécution (mémoire vidée) | Lancer le **ProxyCacheService.exe manuellement**, puis le **RoutingHost** séparément                                                 |
| `AddressAccessDeniedException` au lancement manuel            | Droits insuffisants pour réserver l’URL HTTP                                             | Lancer `.exe` **en administrateur** ou exécuter :<br>`netsh http add urlacl url=http://+:9001/ProxyService user=NOM_UTILISATEUR` |

---

## Bonnes pratiques retenues

* Utiliser un **Proxy générique** pour centraliser les appels HTTP.
* Implémenter le **cache mémoire (MemoryCache)** pour réduire la charge des APIs externes.
* Configurer les **bindings WCF** avec des tailles de message adaptées.
* Lancer les serveurs **sans Visual Studio** via leurs `.exe` (exigence du sujet).

---

# Lancement manuel des serveurs (.exe)

## Objectif

Pouvoir exécuter les serveurs **ProxyCacheService** et **RoutingHost** sans ouvrir Visual Studio,
comme exigé dans le projet (*auto-hébergement*).

---

## Étapes de lancement

### 1. Compiler la solution

Dans Visual Studio :
**Build → Générer la solution** (`Ctrl + Shift + B`)

Les exécutables seront générés dans :

```
ProxyCacheService\bin\Debug\
RoutingHost\bin\Debug\
```

---

### 2. Lancer le ProxyCacheService

1. Ouverture de l’Explorateur de fichiers :
   `ProxyCacheService\bin\Debug\`
2. **Clic droit → Exécuter en tant qu’administrateur** sur
   `ProxyCacheService.exe`
3. On devrait voir :

   ```
   ProxyCacheService started at http://localhost:9001/ProxyService
   Press ENTER to stop...
   ```

💡 Si une erreur `AddressAccessDeniedException` apparaît :

* Soit on relance **en administrateur**,
* Soit on exécute une seule fois cette commande dans un **invite de commandes administrateur** :

  ```bash
  netsh http add urlacl url=http://+:9001/ProxyService user=NOM_UTILISATEUR
  ```

---

### 3. Lancer le RoutingHost

Dans un **nouvel onglet de terminal** ou via double-clic :

```
RoutingHost\bin\Debug\RoutingHost.exe
```

On verra :

```
RoutingService REST démarré !
Test : http://localhost:9002/route?from=Paris&to=Lyon
Appuyez sur Entrée pour arrêter...
```

---

### 4. Tester la communication

Ouvrir le navigateur et accéder à (un test) :

```
http://localhost:9002/route?from=Paris&to=Lyon
```

🧩 **Console Proxy :**

```
[Cache MISS] Fetching https://api.jcdecaux.com/vls/v3/stations?contract=Lyon...
[Cache HIT] https://api.jcdecaux.com/vls/v3/stations?contract=Lyon...
```

🧩 **Console Routing :**

```
[Route] fetching JCDecaux stations for Lyon.
[JCDecaux] raw stations = 350
[Route] stations fetched = 350
```

---

## 🧾 Résumé rapide

| Étape | Action                                      | Port   | Type         |
| ----- | ------------------------------------------- | ------ | ------------ |
| 1     | Lancer `ProxyCacheService.exe`              | `9001` | SOAP         |
| 2     | Lancer `RoutingHost.exe`                    | `9002` | REST         |
| 3     | Accéder à `http://localhost:9002/route?...` | -      | Test complet |

---

## ✅ Bonnes pratiques

* Toujours lancer le **Proxy avant le Routing**.
* Laisser la console du Proxy ouverte pour observer les `[Cache HIT] / [Cache MISS]`.
* Utiliser les `.exe` pour la **démonstration finale** : c’est ce que demandent les consignes du projet.

--- 

## Logging & Gestion des erreurs (Issue #12)

### Objectif
Centraliser et uniformiser les logs du service Proxy pour remplacer les `Console.WriteLine()` dispersés par un système de logging plus lisible et réutilisable.

---

### Implémentation

#### Classe `Logger.cs`
Créée dans le dossier `ProxyCacheService`, cette classe statique gère trois niveaux de logs :
- `Info` → messages informatifs,
- `Warn` → avertissements,
- `Error` → erreurs avec affichage en rouge et option de message d’exception.

```csharp
using System;

namespace ProxyCacheService
{
    internal static class Logger
    {
        public static void Info(string msg) => Console.WriteLine($"[INFO] {DateTime.Now:HH:mm:ss} {msg}");
        public static void Warn(string msg) => Console.WriteLine($"[WARN] {DateTime.Now:HH:mm:ss} {msg}");
        public static void Error(string msg, Exception ex = null)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[ERROR] {DateTime.Now:HH:mm:ss} {msg}");
            if (ex != null) Console.WriteLine($"        → {ex.Message}");
            Console.ResetColor();
        }
    }
}
````

---

### Intégration dans `ProxyService.cs`

Les anciens appels à `Console.WriteLine()` ont été remplacés par des appels à `Logger.Info()` ou `Logger.Error()` pour plus de clarté et une meilleure lisibilité dans la console.

Avant :

```csharp
Console.WriteLine($"[Cache MISS] Fetching {url}");
```

Après :

```csharp
Logger.Info($"[Cache MISS] Fetching {url}");
```

Et pour la gestion des erreurs :

```csharp
catch (Exception ex)
{
    Logger.Error("HTTP Request failed", ex);
    return $"Error fetching {url}: {ex.Message}";
}
```

---

### Exemple de sortie console

```
[INFO] 14:32:05 [Cache MISS] Fetching https://api.jcdecaux.com/vls/v3/stations?contract=Lyon
[INFO] 14:32:07 [Cache HIT] Fetching https://api.jcdecaux.com/vls/v3/stations?contract=Lyon
[ERROR] 14:32:12 HTTP Request failed
        → The remote server returned an error: (403) Forbidden.
```

---

### ✅ Résultats obtenus

* Logs homogènes, datés et lisibles.
* Erreurs colorées pour une meilleure visualisation pendant les tests.
* Centralisation du code de logging → maintenance facilitée.
* Aucun changement fonctionnel sur le comportement du Proxy.

---

### 📘 Bonnes pratiques appliquées

* Ne pas laisser de `Console.WriteLine()` dispersés.
* Préparer la possibilité future d’un logging vers fichier ou d’un niveau `DEBUG`.
* Lancer les tests en mode console pour visualiser les logs en temps réel.

```
