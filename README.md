# Sunseeker Lawn Mower

<img width="713" height="1147" alt="Startseite" src="https://github.com/user-attachments/assets/f7ae803d-2dec-4890-bee9-1988eaa5f1f5" />

## Vorbemerkung

Dieses Projekt ist der Versuch einige Schwächen der von Sunseeker mitgelieferen App zu kompensieren.
- startet man das Mähen manuell, also nicht per Arbeitsplan und schafft der Roboter die angegebene Zone nicht mit einer Akkuladung, so fährt er mit der Arbeit nicht fort nachdem der Akku wieder geladen ist. Das konnte selbst unser alter "Billig"-Mäher besser :-( Anmerkung: ein starrer Wochenplan macht bei ständig wechselndem Wetter aus meiner Sicht keinen Sinn.
- Beim Einlernen, also Abfahren der verbotenen Bereiche kann man sich zwar viel Mühe geben, das Ergebnis sind dennoch nur grobe Flächen mit teilweise deutlich zu weit außen liegenden Ecken.
- hat ein Zone aufgrund mehrerer Kanten eine klare Vorzugsrichtung, so ist es nur sehr schwer möglich diese mit der App zu konfigurieren. Die berechnete Karte ist nicht genordet und mit dem schlecht bedienbaren Winkelcontrol ist eine genaue Einstellung kaum möglich, in jedem Fall aber äußerst mühsam.
- ein automatisiertes über Kreuz Mähen ist nicht möglich. Will heißen bei einem Mähvorgang in eine bestimmte Richtung arbeiten und beim nächsten Mal senkrecht dazu. Manche enge Bereiche sollten nur in einer Richtung bearbeitet werden, da permanentes Wenden die Sache nicht besser macht.
- es fehlt eine einfache Möglichkeit in den aktuellen "Ort des Geschehens" hinein zu zoomen. Ständig von Neuem die Finger zu spreizen langweilt sehr schnell.

## Lösungsansatz

Für den erstgenannten Punkt ist ein Stück Software erforderlich, das permanent arbeitet und den Status des Roboters überwacht. Es gilt zu erkennen, wann der Roboter zumindest wieder ausreichend geladen ist und ob der zuletzt gestartete Job für eine Zone bereits erledigt ist. Es braucht also einen, wenn auch nur kleinen Heimserver. Um solch eine Server basierte Software zu bedienen bietet sich eine Webseite an, die auf praktisch jedem Endgerät nutzbar ist. Für solche Szenarien gibt es zahllose technische Varianten. Hier ist die Entscheidung auf ASP.NET mit C# für die Serverseite und TypeScript mit Vue und Vite für die Clientseite gefallen. Das ganze ist also ein in Visual Studio 2022 mit .NET 10 kompilierbares Projekt.

Da zunächst erst einmal das Kommunikationsprotokoll der vorhandenen App mit der von Sunseeker betriebenen Cloud geklärt werden musste, enthält der Code auch Teile, die zum Verständnis der empfangbaren Daten dient. Dabei war für die ersten Schritte das Projekt [Sunseeker-lawn-mower](https://github.com/Sdahl1234/Sunseeker-lawn-mower) von @Sdahl1234 von großem Nutzen, wofür ich mich hier ausdrücklich bedanken möchte.     

## Weiterentwicklung

Da ich über einen ausgeprägten Spieltrieb verfüge und permanent dabei bin unser Smarthome immer weiter zu perfektionieren, werde ich sicher noch das eine der andere an diesem Projekt weiter entwickeln. Ich würde mich aber sehr freuen, wenn die/der eine oder andere mit Pullrequests, Vorschlägen oder zumindest konstruktiver Kritik ihren/seinen Teil dazu beisteuert. Ich freue mich auf Eure Discussions und Issues.

## Installation und Inbetriebnahme

Da es sich um ein klassisches ASP.NET + Vue.ts Projekt handelt gilt alles, was in der offiziellen Doku dazu zu finden ist. Es braucht ein aktuelles Visual Studio 2022 mit all dem, was zur Enticklung mit C# und TypeScript nötig ist. Aktuell noch zusätzlich die .NET 10 Preview. Die Verwendung von .NET 9 ist auch möglich, allerdings müssen dann ein paar nuget Referenzen auf ältere Versionen umgestellt werden.

Damit sich das System mit dem richtigen Account bei der Sunseeker Cloud anmeldet braucht es noch ein selbst anzulegende Datei: `appsettings.Development.json`. Diese kann als Kopie von `appsettings.json` erzeugt werden. Die Sternchen müssen natürlich entsprechend ersetzt werden.

