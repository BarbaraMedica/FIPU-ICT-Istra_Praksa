# Audio — struktura i postavljanje zvuka

Ovaj dokument opisuje kako je organiziran zvuk u projektu i kako ga builderi automatski povezuju.

## Struktura mapa

```
Assets/Audio/
├── UI/
│   └── button_click.mp3          # klik na gumb (UIAudioManager)
└── SFX/
    ├── Footsteps - Essentials/   # koraci (walk/run po materijalu)
    └── Door, Cabinet and Locker Sound Pack (Free)/   # vrata, ormari, brave
```

## Koraci (FootstepAudio)

Skripta: `Assets/Scripts/Player/FootstepAudio.cs`

Radi samostalno — čita horizontalnu brzinu s `CharacterController`-a, pa ne treba mijenjati
`FirstPersonController`. Nasumično bira isječke, razlikuje hodanje i trčanje te varira visinu
tona da koraci ne zvuče identično.

Oba buildera (`Grade4LevelBuilder`, `EscapeRoomPrototypeBuilder`) automatski dodaju komponentu
igraču i pune je isječcima iz `Footsteps_Tile_Walk` i `Footsteps_Tile_Run`. Ako želiš druge
površine (drvo, šljunak...), promijeni `FootstepWalkFolder` / `FootstepRunFolder` u builderu ili
dodijeli isječke ručno na komponenti u sceni.

## Vrata (EscapeRoomDoor)

Skripta: `Assets/Scripts/Doors/EscapeRoomDoor.cs`

Vrata puštaju zvuk otvaranja, zatvaranja, zaključanih vrata (neuspio pokušaj) i otključavanja.
Builderi dodjeljuju isječke iz door packa. Zvuk je 3D (`spatialBlend = 1`) pa se čuje jače
kad si bliže vratima.

## Ormari (CabinetInteractable)

Skripta: `Assets/Scripts/Interaction/CabinetInteractable.cs`

Zamjenjuje ranije placeholder kocke ormara. Na klik se "otvara"/"zatvara" uz zvuk iz cabinet
packa i može prikazati poruku (npr. trag). Kada nabaviš pravi 3D model ormara, stavi ga kao
child objekt i dodijeli njegov transform polju `doorPivot` — skripta će ga tada i vizualno
otvarati. U `Grade4LevelBuilder` zamijeni `CreateCube` unutar `CreateCabinet` instanciranjem
prefaba.

## Napomena o .meta datotekama

Nove skripte (`FootstepAudio.cs`, `CabinetInteractable.cs`) i nove mape (`Audio/UI`, `Audio/SFX`)
dobit će `.meta` datoteke automatski čim se Unity fokusira na projekt. Nemoj ih ručno stvarati.
