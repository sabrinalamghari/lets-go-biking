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
C’est donc ton pont entre des adresses humaines et des coordonnées GPS utilisables par OSRM.

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
