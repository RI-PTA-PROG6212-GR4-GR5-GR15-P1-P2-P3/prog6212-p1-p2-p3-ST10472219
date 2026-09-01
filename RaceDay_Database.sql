SELECT
    p.FullName AS Participant,
    e.EventName,
    c.CategoryName,
    en.BibNumber
FROM Enrolment en
INNER JOIN Participant p
    ON en.ParticipantID = p.ParticipantID
INNER JOIN Category c
    ON en.CategoryID = c.CategoryID
INNER JOIN Event e
    ON c.EventID = e.EventID;