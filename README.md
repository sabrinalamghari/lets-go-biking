#  LetsGoBiking 

Le but est de calculer des itinéraires optimisés en combinant marche + vélos JCDecaux, avec un serveur REST/SOAP, un proxy/cache, un heavy client, un front web, et un système de notifications en temps réel.

Projet réalisé par : **Sabrina Lamghari** && **Anaïs Lacouture**

---

# 2. Fonctionnalités principales

### Serveur Proxy + Cache 
- Fait les appels HTTP vers :
  - API JCDecaux
  - OSRM (routing)
  - Nominatim API
- Implémente un **cache générique** basé sur MemoryCache pour limiter les appels.
- Exposé via SOAP : `http://localhost:9001/ProxyService`

### Routing Service
- Exposé en **REST** :  
  `GET http://localhost:9002/route?from=...&to=...`
- Exposé aussi en **SOAP** (pour le heavy client).
- Fonctionnalités :
  - Géocodage (Nominatim + fallback)
  - Récupération contrats/stations JCDecaux
  - Sélection des meilleures stations (disponibilité + distance)
  - Calcul marche/vélo via OSRM
  - Modes :
    - `walk_only`
    - `bike+walk`
    - `bike+walk+bike` (inter-ville)
  - Format JSON avec legs, instructions, géométrie.

### ActiveMQ + Notifications météo
- Service C# **ActiveMqProducer** :
  - Récupère les données météo via API réelle (OpenWeatherMap)
  - Envoie des notifications sur un topic ActiveMQ (STOMP)
- Le front écoute en temps réel et affiche des notifications (pluie, vent, etc.).

### Front
- Autocomplete d’adresses (Nominatim).
- Affichage carte Leaflet.
- Tracé marche/vélo en couleurs différentes.
- Instructions détaillées en français.
- Réception notifications temps réel via STOMP (ActiveMQ).
- UI : onglets Carte / Détails + résumé.

### Heavy Client
- Client console C# qui interroge le serveur SOAP.
- Affiche mode, distance, durée, étapes.

---

# 3. Installation & prérequis

### Nécessaires :
- .NET (4.8 ou .NET 6/7/8 selon ton installation)
- ActiveMQ (via Apache ActiveMQ)
  - Console accessible sur `http://localhost:8161`
- Connexion internet :
  - API JCDecaux
  - OSRM (public server)
  - API Météo
- Navigateur moderne (Chrome/Firefox/Edge)

---

# 4. Lancement du projet

## 4.1 Lancement automatisé via `launcher.bat`

Le fichier `launcher.bat` se trouve dans : LetsGoBiking/launcher.bat



