# DECISIONS

## 1. O regulă pe care am mutat-o
Am mutat regula R8 (interzicerea anulării unei rezervări care este deja anulată) și regula R7 (verificarea suprapunerii intervalelor) din `Services/BookingService.cs` în `Domain/Bookings/Booking.cs` (`Booking.Cancel`) și `Domain/Bookings/DateRange.cs` (`DateRange.Overlaps`). Mutându-le în domeniu, entitatea își protejează singură starea și niciun handler nu poate schimba `Status` fără să treacă prin aceste reguli.

## 2. Result în loc de excepție
Faptul că o sală este ocupată este un caz normal de business, nu o eroare tehnică neașteptată, așa că `Result<T>` face eșecul vizibil direct în semnătura metodei, fără costul aruncării unei excepții. Când `CreateBookingHandler` întoarce `BookingErrors.RoomOccupied` (`ErrorType.Conflict`), `CreateBookingEndpoint` apelează `result.Error.ToProblem()` (`Common/Http/ErrorHttpExtensions.cs`), care îl transformă într-un răspuns JSON `ProblemDetails` cu status 409 Conflict.

## 3. Cine știe de EF Core?
Doar clasele din `Infrastructure/Persistence` (`RoomHubDbContext`, `EntityConfigurations`, `RoomRepository`, `BookingRepository`) și endpoint-urile de citire (`GetRoomByIdEndpoint`, `GetBookingByIdEndpoint`) știu de EF Core. Stratul `Domain` și handler-ele de comenzi folosesc doar interfețele `IRoomRepository`, `IBookingRepository` și `IUnitOfWork`. Câștigăm o logică de domeniu decuplată și ușor de testat, iar costul este scrierea unor interfețe și clase de repository în plus.

## 4. Fluxul unei cereri
`CreateBookingEndpoint.Handle` (`Features/Bookings/CreateBooking/CreateBookingFeature.cs`) -> `CreateBookingHandler.HandleAsync` -> `RoomRepository.GetByIdAsync` (`Infrastructure/Persistence/Repositories/Repositories.cs`) -> `DateRange.Create` și `Booking.Create` (`Domain/Bookings/`) -> `BookingRepository.GetActiveForRoomAsync` și `CountActiveForParticipantOnDayAsync` -> `BookingRepository.Add` -> `RoomHubDbContext.SaveChangesAsync` (`IUnitOfWork`) -> SQLite (`roomhub.db`).

## 5. Ce am respins sau corectat din ce a propus AI-ul
Inițial, AI-ul a pus limita de 4 ore (R6) direct în `DateRange.Create`, ceea ce nu mai funcționa după apariția cerinței R10 (sălile cu capacitate peste 10 au limită de 2 ore). Am mutat verificarea duratei din `DateRange.Create` în `Booking.Create`, unde avem acces la `room.Capacity`, lăsând `DateRange` să se ocupe doar de validarea capetelor intervalului și de `Overlaps`.

## 6. Dacă aș mai avea 30 de minute
->Aș adăuga protecție la concurență (de exemplu o tranzacție sau verificare optimistă) pentru verificările de suprapunere și limita zilnică din `CreateBookingHandler`, astfel încât două cereri simultane să nu poată rezerva același interval.
->De asemenea, aș scrie teste automate de integrare pentru toate regulile R1–R11.