# Prototip Escape Rooma

Projekt uključuje Unity editor alat koji iz primitivne geometrije gradi igrivi obrazovni escape-room prototip iz prvog lica.

## Izgradnja Scene

1. Otvori projekt u Unityju 6.
2. Pričekaj da se skripte prevedu.
3. Pokreni `Tools > Escape Room > Auto Setup All Scenes`.
4. Otvori `Assets/Scenes/EscapeRoomPrototype.unity` ako Unity ne prijeđe na scenu automatski.
5. Pritisni Play.

Naredba za automatsko postavljanje ponovno gradi generiranu scenu prototipa, povezuje igrača, UI, vrata, interaktivne predmete i zagonetke, a zatim registrira sve scene iz mape `Assets/Scenes` u Build Settings. Sigurno ju je pokrenuti prije uređivanja jer regenerira samo namjensku scenu prototipa i generirane pomoćne assete.

Svaki generirani tekst za interakciju uključuje oznaku tipke, na primjer `Povuci polugu izlaza (E)`, kroz polje `PlayerInteractor.interactKeyHint` na generiranom igraču.

Generirana scena ne stavlja tekst zagonetki na zidove niti iznad odgovora. Vrata koriste kompaktne, čvrste `?` oznake bez tekstualnog sjaja, blokovi odgovora imaju kompaktne `!` oznake na gornjoj strani, a puni tekst pitanja i odgovora prikazuje se u promptu ili skočnom okviru.

Pritiskom na Escape igra se pauzira, prikazuje se `Pauza`, a tekst traži `Pritisni Esc za nastavak`.

Možeš pokrenuti i `Tools > Escape Room > Build Prototype Scene` ako želiš osvježiti samo scenu prototipa, ili `Tools > Escape Room > Register All Scenes In Build Settings` ako želiš ažurirati samo registraciju scena.

## Kontrole

- Kretanje (`WASD`)
- Pogled (miš)
- Interakcija (`E`)
- Skok (Space)
- Pauza / nastavak (Escape)
- Lijevi klik mišem ponovno zaključava kursor kada igra nije pauzirana

## Tijek Prototipa

- Igra počinje u uvodnoj sobi.
- Pregledaj svjetleću kocku kako bi provjerio interakciju.
- Povuci polugu za otključavanje izlaza iz uvodne sobe.
- Uđi u središnju sobu i odaberi jedna od pet predmetnih vrata.
- Pogledaj vrata s oznakom `?` kako bi pročitao pitanje u skočnom okviru.
- Pogledaj blokove s oznakom `!` kako bi vidio odgovore, zatim pritisni `E` na točan blok.
- Riješi zagonetku u sobi kako bi otvorio završna vrata.
- Svaka vrata imaju kvake s obje strane i mogu se otvoriti ili zatvoriti s ispravne strane šarki.

## Generirani Sustavi

- `FirstPersonController`: kretanje kroz CharacterController, pogled mišem, gravitacija i opcionalni skok.
- `PlayerInteractor`: interakcija zrakom iz središta ekrana s bilo kojim `IInteractable` objektom.
- `GameUIController`: nišan, tekst interakcije, tekst cilja i panel poruka.
- `EscapeRoomDoor`: zaključana i otključana vrata s animacijom otvaranja i zatvaranja.
- `PuzzleBase`: bazna klasa za stanje riješenosti i događaj rješavanja.
- `MultipleChoicePuzzle`: obrazovna zagonetka s višestrukim izborom.
- `SequencePuzzle`: zagonetka s pravilnim redoslijedom gumba.

## Proširenje Prototipa

Za kasnije dodavanje nove sobe:

1. Dodaj ljusku sobe u `EscapeRoomPrototypeBuilder` pomoću `CreateRoom`.
2. Dodaj otvor u središnjoj sobi i predmetna vrata pomoću `CreateSubjectDoor`.
3. Napravi upravitelj zagonetke koji nasljeđuje `PuzzleBase` ili ponovno upotrijebi `MultipleChoicePuzzle` / `SequencePuzzle`.
4. Dodijeli zagonetku u polje `EscapeRoomDoor.requiredPuzzle` kako bi se vrata otključala kada je zagonetka riješena.

Scena koristi standardni Unity UI/Text i male 3D oznake simbola, pa radi bez TextMeshPro paketa.