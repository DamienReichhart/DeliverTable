# Wireframes - DeliverTable

Ce document présente la structure (wireframes) des pages clés de l'application, basée sur les maquettes et captures d'écran du projet.

---

## 1. Détails du Restaurant (Basé sur la capture d'écran)

Ce wireframe représente la vue d'un client consultant la carte d'un établissement.

```mermaid
graph TD
    subgraph "Page : Détails Restaurant"
        subgraph "Sidebar (Gauche)"
            S1[Logo DeliverTable]
            S2[Navigation: Restaurants]
            S3[Mon Profil]
            S4[Mes Commandes]
            S5[Déconnexion]
        end

        subgraph "Contenu Principal (Droite)"
            subgraph "En-tête Restaurant"
                H1[Badge: Type de cuisine]
                H2[Nom du Restaurant]
                H3[Slogan / Description]
                H4[Adresse]
                subgraph "Widget Panier (Flottant)"
                    P1[Nb Articles - Prix Total]
                    P2[Bouton: Voir le panier]
                end
            end

            subgraph "Section: La Carte"
                F1[Barre de recherche de plats]
                F2[Filtres: Végétarien / Végan / Sans gluten]
                
                subgraph "Grille de Plats"
                    D1[Image du plat]
                    D2[Nom + Description]
                    D3[Badge Allergènes]
                    D4[Prix + Sélecteur Quantité + Bouton Ajout]
                end
            end
        end
    end
```

---

## 2. Page d'Accueil / Recherche de Restaurants

Structure de la page permettant la découverte des établissements.

```mermaid
graph TD
    subgraph "Page : Accueil & Recherche"
        subgraph "Header"
            H[Barre de navigation / Recherche globale]
        end
        
        subgraph "Hero Section"
            HT[Titre Accrocheur]
            HS[Input: Localisation / Type de cuisine]
        end

        subgraph "Résultats de Recherche"
            subgraph "Filtres Latéraux"
                FL1[Note minimale]
                FL2[Budget]
                FL3[Distance]
            end
            
            subgraph "Grille de Restaurants"
                R1[Carte: Image + Nom + Note + Spécialité]
                R2[Carte: Image + Nom + Note + Spécialité]
            end
        end
    end
```

---

## 3. Dashboard Administrateur

Vue consolidée pour la modération et le suivi du système.

```mermaid
graph TD
    subgraph "Page : Admin Dashboard"
        subgraph "Statistiques (Top)"
            ST1[Total Commandes]
            ST2[Chiffre d'Affaires]
            ST3[Nouveaux Inscrits]
        end

        subgraph "Flux de Modération"
            M1[Liste: Restaurants à valider]
            M2[Liste: Avis signalés]
            M3[Liste: Réclamations en attente]
        end

        subgraph "Graphiques"
            G1[Évolution des ventes - 30 jours]
        end
    end
```

---

## 4. Authentification (Login)

```mermaid
graph TD
    subgraph "Page : Connexion"
        subgraph "Container Central"
            L1[Logo]
            L2[Champ: Email]
            L3[Champ: Mot de passe]
            L4[Bouton: Se connecter]
            L5[Lien: Mot de passe oublié]
            L6[Lien: Créer un compte]
        end
    end
```
