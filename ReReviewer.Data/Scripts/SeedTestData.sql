/*
    Тестовые данные ReReviewer — только для разработки.

    Запускать на базе, к которой уже применены миграции:
        dotnet ef database update --project ReReviewer.Data --startup-project ReReviewer.Server
        sqlcmd -S localhost -E -C -d ReReviewerDB -f 65001 -i ReReviewer.Data/Scripts/SeedTestData.sql
    Ключ -f 65001 нужен, чтобы sqlcmd прочитал файл как UTF-8, иначе испортится кириллица.

    Пользователи, у всех пароль ReReviewer-Test-1:
        admin@rereviewer.test      — роль Admin
        moderator@rereviewer.test  — роль Moderator
        ivan@rereviewer.test       — аккаунт, привязанный к обзорщику «Иван «PixelHunter» Петров»
        alex@, maria@, dmitry@, olga@rereviewer.test — обычные пользователи

    Обзорщики, издания и ссылки на материалы вымышленные (example.com), игры — реальные.
    Всё выполняется в одной транзакции: при любой ошибке база не меняется.
    Повторный запуск ничего не делает, если тестовые пользователи уже есть.
    Роли, жанры, бейджи и критерии с уже существующими названиями не дублируются.

    Удалить эти данные — DeleteTestData.sql в этой же папке.
*/

-- sqlcmd по умолчанию выключает QUOTED_IDENTIFIER, а без него SQL Server не даёт писать в таблицы
-- с фильтрованными индексами (Reviewers.UserId). Отдельный пакет — чтобы настройки действовали на весь скрипт.
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;

IF EXISTS (SELECT 1 FROM Users WHERE Email LIKE N'%@rereviewer.test')
BEGIN
    PRINT N'Тестовые данные уже загружены, скрипт ничего не изменил.';
    RETURN;
END;

BEGIN TRANSACTION;

-----------------------------------------------------------------------------------------------
-- Справочники: роли, жанры, бейджи, критерии
-----------------------------------------------------------------------------------------------

INSERT INTO Roles (Title)
SELECT v.Title
FROM (VALUES (N'Admin'), (N'Moderator')) AS v(Title)
WHERE NOT EXISTS (SELECT 1 FROM Roles r WHERE r.Title = v.Title);

DECLARE @roleAdmin     INT = (SELECT Id FROM Roles WHERE Title = N'Admin');
DECLARE @roleModerator INT = (SELECT Id FROM Roles WHERE Title = N'Moderator');

INSERT INTO Genres (Title)
SELECT v.Title
FROM (VALUES (N'RPG'), (N'Action'), (N'Shooter'), (N'Strategy'), (N'Indie'), (N'Horror')) AS v(Title)
WHERE NOT EXISTS (SELECT 1 FROM Genres g WHERE g.Title = v.Title);

DECLARE @gRpg      INT = (SELECT Id FROM Genres WHERE Title = N'RPG');
DECLARE @gAction   INT = (SELECT Id FROM Genres WHERE Title = N'Action');
DECLARE @gShooter  INT = (SELECT Id FROM Genres WHERE Title = N'Shooter');
DECLARE @gStrategy INT = (SELECT Id FROM Genres WHERE Title = N'Strategy');
DECLARE @gIndie    INT = (SELECT Id FROM Genres WHERE Title = N'Indie');
DECLARE @gHorror   INT = (SELECT Id FROM Genres WHERE Title = N'Horror');

INSERT INTO Badges (Title)
SELECT v.Title
FROM (VALUES (N'Объективный'), (N'Глубокий разбор'), (N'Без спойлеров'), (N'Ветеран индустрии')) AS v(Title)
WHERE NOT EXISTS (SELECT 1 FROM Badges b WHERE b.Title = v.Title);

DECLARE @bObjective  INT = (SELECT Id FROM Badges WHERE Title = N'Объективный');
DECLARE @bDeep       INT = (SELECT Id FROM Badges WHERE Title = N'Глубокий разбор');
DECLARE @bNoSpoilers INT = (SELECT Id FROM Badges WHERE Title = N'Без спойлеров');
DECLARE @bVeteran    INT = (SELECT Id FROM Badges WHERE Title = N'Ветеран индустрии');

-- Четыре общих критерия (GenreId = NULL) и три жанровых.
INSERT INTO Criteria (Title, GenreId)
SELECT v.Title, v.GenreId
FROM (VALUES
    (N'Объективность', NULL),
    (N'Глубина анализа', NULL),
    (N'Подача', NULL),
    (N'Знание игры', NULL),
    (N'Разбор билдов', @gRpg),
    (N'Анализ баланса', @gStrategy),
    (N'Атмосфера и напряжение', @gHorror)
) AS v(Title, GenreId)
WHERE NOT EXISTS (
    SELECT 1 FROM Criteria c
    WHERE c.Title = v.Title AND (c.GenreId = v.GenreId OR (c.GenreId IS NULL AND v.GenreId IS NULL)));

DECLARE @cObjectivity INT = (SELECT TOP 1 Id FROM Criteria WHERE Title = N'Объективность' AND GenreId IS NULL ORDER BY Id);
DECLARE @cDepth       INT = (SELECT TOP 1 Id FROM Criteria WHERE Title = N'Глубина анализа' AND GenreId IS NULL ORDER BY Id);
DECLARE @cDelivery    INT = (SELECT TOP 1 Id FROM Criteria WHERE Title = N'Подача' AND GenreId IS NULL ORDER BY Id);
DECLARE @cKnowledge   INT = (SELECT TOP 1 Id FROM Criteria WHERE Title = N'Знание игры' AND GenreId IS NULL ORDER BY Id);
DECLARE @cBuilds      INT = (SELECT TOP 1 Id FROM Criteria WHERE Title = N'Разбор билдов' AND GenreId = @gRpg ORDER BY Id);
DECLARE @cBalance     INT = (SELECT TOP 1 Id FROM Criteria WHERE Title = N'Анализ баланса' AND GenreId = @gStrategy ORDER BY Id);
DECLARE @cAtmosphere  INT = (SELECT TOP 1 Id FROM Criteria WHERE Title = N'Атмосфера и напряжение' AND GenreId = @gHorror ORDER BY Id);

-----------------------------------------------------------------------------------------------
-- Пользователи
-----------------------------------------------------------------------------------------------

DECLARE @uAdmin     UNIQUEIDENTIFIER = '11111111-0000-0000-0000-000000000001';
DECLARE @uModerator UNIQUEIDENTIFIER = '11111111-0000-0000-0000-000000000002';
DECLARE @uIvan      UNIQUEIDENTIFIER = '11111111-0000-0000-0000-000000000003';
DECLARE @uAlex      UNIQUEIDENTIFIER = '11111111-0000-0000-0000-000000000004';
DECLARE @uMaria     UNIQUEIDENTIFIER = '11111111-0000-0000-0000-000000000005';
DECLARE @uDmitry    UNIQUEIDENTIFIER = '11111111-0000-0000-0000-000000000006';
DECLARE @uOlga      UNIQUEIDENTIFIER = '11111111-0000-0000-0000-000000000007';

-- Хэши — PBKDF2 из ReReviewer.Application.Security.PasswordHasher для пароля ReReviewer-Test-1.
INSERT INTO Users (Id, Name, Email, PasswordHash, CreatedAt) VALUES
    (@uAdmin,     N'admin',       N'admin@rereviewer.test',     N'210000.3juj4WdyYHMRsLDi+JtzgA==.R4G1NFjANz6PvjyXC5ELe+j6VVmw4SmnimD84oii9FbkDFcHXjwPSatY5++SpnXEte5rizQPm4NQa3LJd4bp2Q==',     '2026-07-01T09:00:00'),
    (@uModerator, N'moderator',   N'moderator@rereviewer.test', N'210000.beThaTgac4iBPvijDv7c/w==.Ec8YdrOg+xEptZNqo7scw28wa+KYouzd9Rss9wxVKoZbW7XemfEatPTrKcmydQ3qIpVQh4puFjCJgSLBuWUyeg==', '2026-07-01T09:05:00'),
    (@uIvan,      N'Иван Петров', N'ivan@rereviewer.test',      N'210000.YXlG6hB5D5JLrPvaYeGIwQ==.cTtYr1NWfFsw+s0uuFC0jDRv8lcYx87qZGQBAf+uHM9N/LRhcxZzk2fSavyWHIMrkpnBSb39LGxJRoLrkfmkuQ==',      '2026-07-15T12:30:00'),
    (@uAlex,      N'Алексей',     N'alex@rereviewer.test',      N'210000.uEqLr/Kbvm9EFveY41AySw==.2KKeZTsWvzP5LBpfcvAKVuKT0OA5vwxzvrZkxpEl8xzoQkJl+5bhokshgdsfxOxsujyxaNxD531hq2YnKcRb/Q==',      '2026-08-02T18:10:00'),
    (@uMaria,     N'Мария',       N'maria@rereviewer.test',     N'210000.c8fIJAg7C1gQc2RzJw6xmg==.gt9o0KbBx2U78p3ytrU0Xh10srE00sUR5XZ0/1I2ErLheuzu7YwsBx0njTDrgjAqMHazBRbtMi0XyYrkINP8hw==',     '2026-08-05T10:45:00'),
    (@uDmitry,    N'Дмитрий',     N'dmitry@rereviewer.test',    N'210000.uqFcul9SEG0fKB/znxakuw==.UobegLaCysJ/FpcLD2sA3KLItgVf93waAFpDKu1Vqq21wXXn514W0w3SNs9rfI1/uZlagNYcTqMcqiXe0xMg/Q==',    '2026-08-20T21:00:00'),
    (@uOlga,      N'Ольга',       N'olga@rereviewer.test',      N'210000.vWaUuHO/V8XL9InLQEiRlQ==.ZFalUM7S3cl6kGEiDfGuwvD3OJl9Yf2WLj0hP65nrtFAzSBBwOpC7FxL9H0d96XiEv3Xo7TsPYAg3ULMmZCLPw==',      '2026-08-25T15:20:00');

INSERT INTO UserRoles (RolesId, UsersId) VALUES
    (@roleAdmin,     @uAdmin),
    (@roleModerator, @uModerator);

-----------------------------------------------------------------------------------------------
-- Издания и игры
-----------------------------------------------------------------------------------------------

DECLARE @oVestnik UNIQUEIDENTIFIER = '33333333-0000-0000-0000-000000000001';
DECLARE @oPixel   UNIQUEIDENTIFIER = '33333333-0000-0000-0000-000000000002';
DECLARE @oKitchen UNIQUEIDENTIFIER = '33333333-0000-0000-0000-000000000003';

INSERT INTO Outlets (Id, Name) VALUES
    (@oVestnik, N'Игровой вестник'),
    (@oPixel,   N'Пиксельный обзор'),
    (@oKitchen, N'Геймерская кухня');

DECLARE @gmEldenRing    UNIQUEIDENTIFIER = '44444444-0000-0000-0000-000000000001';
DECLARE @gmBg3          UNIQUEIDENTIFIER = '44444444-0000-0000-0000-000000000002';
DECLARE @gmCyberpunk    UNIQUEIDENTIFIER = '44444444-0000-0000-0000-000000000003';
DECLARE @gmDoom         UNIQUEIDENTIFIER = '44444444-0000-0000-0000-000000000004';
DECLARE @gmCiv6         UNIQUEIDENTIFIER = '44444444-0000-0000-0000-000000000005';
DECLARE @gmHades        UNIQUEIDENTIFIER = '44444444-0000-0000-0000-000000000006';
DECLARE @gmHollowKnight UNIQUEIDENTIFIER = '44444444-0000-0000-0000-000000000007';
DECLARE @gmRe4          UNIQUEIDENTIFIER = '44444444-0000-0000-0000-000000000008';
DECLARE @gmDeadSpace    UNIQUEIDENTIFIER = '44444444-0000-0000-0000-000000000009';
DECLARE @gmStardew      UNIQUEIDENTIFIER = '44444444-0000-0000-0000-000000000010';

-- Игра может остаться от прошлой загрузки: DeleteTestData.sql не удаляет игры, на которые ссылаются ваши материалы.
INSERT INTO Games (Id, Title, Description)
SELECT v.Id, v.Title, v.Description
FROM (VALUES
    (@gmEldenRing,    N'Elden Ring',           N'Ролевой экшен в открытом мире от FromSoftware.'),
    (@gmBg3,          N'Baldur''s Gate 3',     N'Партийная RPG по Dungeons & Dragons с пошаговыми боями.'),
    (@gmCyberpunk,    N'Cyberpunk 2077',       N'Ролевой шутер в открытом мире Найт-Сити.'),
    (@gmDoom,         N'Doom Eternal',         N'Динамичный шутер от первого лица про битву с демонами.'),
    (@gmCiv6,         N'Civilization VI',      N'Пошаговая глобальная стратегия о развитии цивилизации.'),
    (@gmHades,        N'Hades',                N'Рогалик с боями в реальном времени по мотивам греческих мифов.'),
    (@gmHollowKnight, N'Hollow Knight',        N'Метроидвания с ручной рисовкой и высокой сложностью.'),
    (@gmRe4,          N'Resident Evil 4',      N'Ремейк классического хоррор-экшена.'),
    (@gmDeadSpace,    N'Dead Space',           N'Ремейк научно-фантастического хоррора на космической станции.'),
    (@gmStardew,      N'Stardew Valley',       NULL)
) AS v(Id, Title, Description)
WHERE NOT EXISTS (SELECT 1 FROM Games g WHERE g.Id = v.Id);

INSERT INTO GameGenres (GamesId, GenresId)
SELECT v.GamesId, v.GenresId
FROM (VALUES
    (@gmEldenRing, @gRpg), (@gmEldenRing, @gAction),
    (@gmBg3, @gRpg), (@gmBg3, @gStrategy),
    (@gmCyberpunk, @gRpg), (@gmCyberpunk, @gShooter),
    (@gmDoom, @gShooter), (@gmDoom, @gAction),
    (@gmCiv6, @gStrategy),
    (@gmHades, @gIndie), (@gmHades, @gAction),
    (@gmHollowKnight, @gIndie), (@gmHollowKnight, @gAction),
    (@gmRe4, @gHorror), (@gmRe4, @gAction),
    (@gmDeadSpace, @gHorror), (@gmDeadSpace, @gShooter),
    (@gmStardew, @gIndie)
) AS v(GamesId, GenresId)
WHERE NOT EXISTS (SELECT 1 FROM GameGenres gg WHERE gg.GamesId = v.GamesId AND gg.GenresId = v.GenresId);

-----------------------------------------------------------------------------------------------
-- Обзорщики: жанры, издания, бейджи
-----------------------------------------------------------------------------------------------

DECLARE @rIvan   UNIQUEIDENTIFIER = '22222222-0000-0000-0000-000000000001';
DECLARE @rAnna   UNIQUEIDENTIFIER = '22222222-0000-0000-0000-000000000002';
DECLARE @rMaxim  UNIQUEIDENTIFIER = '22222222-0000-0000-0000-000000000003';
DECLARE @rKate   UNIQUEIDENTIFIER = '22222222-0000-0000-0000-000000000004';
DECLARE @rSergey UNIQUEIDENTIFIER = '22222222-0000-0000-0000-000000000005';
DECLARE @rNikita UNIQUEIDENTIFIER = '22222222-0000-0000-0000-000000000006';

-- Профиль Ивана подтверждён и привязан к аккаунту ivan@rereviewer.test.
INSERT INTO Reviewers (Id, FullName, UserId) VALUES
    (@rIvan,   N'Иван «PixelHunter» Петров', @uIvan),
    (@rAnna,   N'Анна Смирнова',             NULL),
    (@rMaxim,  N'Максим «Hardcore» Орлов',   NULL),
    (@rKate,   N'Екатерина Волкова',         NULL),
    (@rSergey, N'Сергей «Стратег» Козлов',   NULL),
    (@rNikita, N'Никита Морозов',            NULL);

INSERT INTO ReviewerGenres (GenresId, ReviewersId) VALUES
    (@gRpg, @rIvan), (@gAction, @rIvan),
    (@gStrategy, @rAnna), (@gIndie, @rAnna),
    (@gShooter, @rMaxim), (@gAction, @rMaxim),
    (@gHorror, @rKate), (@gIndie, @rKate),
    (@gStrategy, @rSergey),
    (@gRpg, @rNikita);

-- Максим перешёл из «Игрового вестника» в «Геймерскую кухню»; Екатерина и Никита — независимые авторы.
INSERT INTO ReviewerOutlets (ReviewerId, OutletId, StartedAt, EndedAt) VALUES
    (@rIvan,   @oVestnik, '2019-03-01', NULL),
    (@rAnna,   @oPixel,   '2015-09-01', NULL),
    (@rMaxim,  @oVestnik, '2016-01-15', '2021-06-30'),
    (@rMaxim,  @oKitchen, '2021-07-01', NULL),
    (@rSergey, @oPixel,   '2017-01-10', NULL);

INSERT INTO ReviewerBadges (ReviewerId, BadgeId, AwardedAt) VALUES
    (@rIvan,  @bObjective,  '2026-09-01T00:00:00'),
    (@rAnna,  @bDeep,       '2026-09-01T00:00:00'),
    (@rAnna,  @bObjective,  '2026-10-01T00:00:00'),
    (@rMaxim, @bVeteran,    '2026-09-01T00:00:00'),
    (@rKate,  @bNoSpoilers, '2026-10-01T00:00:00');

-----------------------------------------------------------------------------------------------
-- Материалы обзорщиков (OriginalScore — оценка игре по шкале 0–10, NULL — без оценки)
-----------------------------------------------------------------------------------------------

DECLARE @m01 UNIQUEIDENTIFIER = '55555555-0000-0000-0000-000000000001';
DECLARE @m02 UNIQUEIDENTIFIER = '55555555-0000-0000-0000-000000000002';
DECLARE @m03 UNIQUEIDENTIFIER = '55555555-0000-0000-0000-000000000003';
DECLARE @m04 UNIQUEIDENTIFIER = '55555555-0000-0000-0000-000000000004';
DECLARE @m05 UNIQUEIDENTIFIER = '55555555-0000-0000-0000-000000000005';
DECLARE @m06 UNIQUEIDENTIFIER = '55555555-0000-0000-0000-000000000006';
DECLARE @m07 UNIQUEIDENTIFIER = '55555555-0000-0000-0000-000000000007';
DECLARE @m08 UNIQUEIDENTIFIER = '55555555-0000-0000-0000-000000000008';
DECLARE @m09 UNIQUEIDENTIFIER = '55555555-0000-0000-0000-000000000009';
DECLARE @m10 UNIQUEIDENTIFIER = '55555555-0000-0000-0000-000000000010';
DECLARE @m11 UNIQUEIDENTIFIER = '55555555-0000-0000-0000-000000000011';
DECLARE @m12 UNIQUEIDENTIFIER = '55555555-0000-0000-0000-000000000012';
DECLARE @m13 UNIQUEIDENTIFIER = '55555555-0000-0000-0000-000000000013';
DECLARE @m14 UNIQUEIDENTIFIER = '55555555-0000-0000-0000-000000000014';
DECLARE @m15 UNIQUEIDENTIFIER = '55555555-0000-0000-0000-000000000015';
DECLARE @m16 UNIQUEIDENTIFIER = '55555555-0000-0000-0000-000000000016';
DECLARE @m17 UNIQUEIDENTIFIER = '55555555-0000-0000-0000-000000000017';

INSERT INTO Materials (Id, ReviewerId, GameId, OutletId, Title, Url, Format, OriginalScore, PublishedAt, CreatedAt) VALUES
    (@m01, @rIvan,   @gmEldenRing,    @oVestnik, N'Обзор Elden Ring: лучшая игра FromSoftware?',        N'https://example.com/vestnik/elden-ring-review',       N'Video',   9.5,  '2022-03-05', '2026-08-01T10:00:00'),
    (@m02, @rIvan,   @gmBg3,          @oVestnik, N'Baldur''s Gate 3 — новый стандарт ролевых игр',      N'https://example.com/vestnik/baldurs-gate-3',          N'Article', 9.0,  '2023-08-10', '2026-08-01T10:05:00'),
    (@m03, @rIvan,   @gmCyberpunk,    NULL,      N'Cyberpunk 2077 после патча 2.0',                     N'https://example.com/pixelhunter/cyberpunk-2-0',       N'Video',   8.0,  '2023-10-01', '2026-08-01T10:10:00'),
    (@m04, @rIvan,   @gmEldenRing,    NULL,      N'Стрим: проходим Shadow of the Erdtree',              N'https://example.com/pixelhunter/erdtree-stream',      N'Stream',  NULL, '2024-06-25', '2026-08-01T10:15:00'),
    (@m05, @rAnna,   @gmCiv6,         @oPixel,   N'Civilization VI: стоит ли переходить с пятой части', N'https://example.com/pixel/civilization-6',             N'Article', 8.5,  '2016-11-01', '2026-08-02T11:00:00'),
    (@m06, @rAnna,   @gmStardew,      NULL,      N'Подкаст: почему Stardew Valley не отпускает',        N'https://example.com/anna/stardew-podcast',            N'Podcast', NULL, '2020-05-12', '2026-08-02T11:05:00'),
    (@m07, @rAnna,   @gmBg3,          @oPixel,   N'Тактика в Baldur''s Gate 3: разбор боёвки',          N'https://example.com/pixel/bg3-tactics',               N'Video',   9.5,  '2023-08-15', '2026-08-02T11:10:00'),
    (@m08, @rMaxim,  @gmDoom,         @oVestnik, N'Doom Eternal — чистый адреналин',                    N'https://example.com/vestnik/doom-eternal',            N'Video',   9.0,  '2020-03-25', '2026-08-03T09:00:00'),
    (@m09, @rMaxim,  @gmCyberpunk,    @oVestnik, N'Cyberpunk 2077 на релизе: разочарование года',       N'https://example.com/vestnik/cyberpunk-launch',        N'Article', 5.5,  '2020-12-15', '2026-08-03T09:05:00'),
    (@m10, @rMaxim,  @gmDeadSpace,    @oKitchen, N'Ремейк Dead Space: снова страшно',                   N'https://example.com/kitchen/dead-space-remake',       N'Video',   8.0,  '2023-02-01', '2026-08-03T09:10:00'),
    (@m11, @rKate,   @gmRe4,          NULL,      N'Resident Evil 4 Remake — разбор атмосферы',          N'https://example.com/kate/re4-remake',                 N'Video',   9.0,  '2023-03-30', '2026-08-04T14:00:00'),
    (@m12, @rKate,   @gmDeadSpace,    NULL,      N'Стрим: Dead Space вслепую',                          N'https://example.com/kate/dead-space-stream',          N'Stream',  NULL, '2023-01-28', '2026-08-04T14:05:00'),
    (@m13, @rKate,   @gmHollowKnight, NULL,      N'Hollow Knight: метроидвания без компромиссов',       N'https://example.com/kate/hollow-knight',              N'Article', 10.0, '2019-04-02', '2026-08-04T14:10:00'),
    (@m14, @rSergey, @gmCiv6,         @oPixel,   N'Civilization VI спустя год: баланс и дополнения',    N'https://example.com/pixel/civ6-year-later',           N'Video',   7.0,  '2017-02-14', '2026-08-05T16:00:00'),
    (@m15, @rSergey, @gmBg3,          NULL,      N'Подкаст: Baldur''s Gate 3 глазами стратега',         N'https://example.com/sergey/bg3-podcast',              N'Podcast', 8.0,  '2023-09-05', '2026-08-05T16:05:00'),
    (@m16, @rNikita, @gmHades,        NULL,      N'Стрим: Hades до финала за вечер',                    N'https://example.com/nikita/hades-stream',             N'Stream',  NULL, '2020-10-01', '2026-08-06T20:00:00'),
    (@m17, @rNikita, @gmEldenRing,    NULL,      N'Elden Ring: обзор для новичков',                     N'https://example.com/nikita/elden-ring-beginners',     N'Video',   7.5,  '2022-03-20', '2026-08-06T20:05:00');

-----------------------------------------------------------------------------------------------
-- Отзывы пользователей на обзорщиков: опубликованные, на модерации и отклонённый
-----------------------------------------------------------------------------------------------

DECLARE @v01 UNIQUEIDENTIFIER = '66666666-0000-0000-0000-000000000001';
DECLARE @v02 UNIQUEIDENTIFIER = '66666666-0000-0000-0000-000000000002';
DECLARE @v03 UNIQUEIDENTIFIER = '66666666-0000-0000-0000-000000000003';
DECLARE @v04 UNIQUEIDENTIFIER = '66666666-0000-0000-0000-000000000004';
DECLARE @v05 UNIQUEIDENTIFIER = '66666666-0000-0000-0000-000000000005';
DECLARE @v06 UNIQUEIDENTIFIER = '66666666-0000-0000-0000-000000000006';
DECLARE @v07 UNIQUEIDENTIFIER = '66666666-0000-0000-0000-000000000007';
DECLARE @v08 UNIQUEIDENTIFIER = '66666666-0000-0000-0000-000000000008';
DECLARE @v09 UNIQUEIDENTIFIER = '66666666-0000-0000-0000-000000000009';
DECLARE @v10 UNIQUEIDENTIFIER = '66666666-0000-0000-0000-000000000010';
DECLARE @v11 UNIQUEIDENTIFIER = '66666666-0000-0000-0000-000000000011';
DECLARE @v12 UNIQUEIDENTIFIER = '66666666-0000-0000-0000-000000000012';
DECLARE @v13 UNIQUEIDENTIFIER = '66666666-0000-0000-0000-000000000013';

-- Не больше одного отзыва от пользователя на обзорщика (уникальный индекс UserId + ReviewerId).
INSERT INTO Reviews (Id, UserId, ReviewerId, Text, Score, Status, CreatedAt, UpdatedAt) VALUES
    (@v01, @uAlex,   @rIvan,   N'Разбирает механику без воды: после его обзора Elden Ring я наконец понял, как работает прокачка. Оценки взвешенные, недостатки игр не замалчивает.', 9,  N'Published', '2026-08-10T18:20:00', NULL),
    (@v02, @uMaria,  @rIvan,   N'Видео хорошие, но в ролике про Cyberpunk 2077 слишком много спойлеров сюжета — предупреждать бы заранее.',                                     7,  N'Published', '2026-08-15T12:05:00', NULL),
    (@v03, @uDmitry, @rIvan,   N'Ставит слишком мягкие оценки крупным студиям, к небольшим играм заметно строже.',                                                                 4,  N'Pending',   '2026-10-20T21:40:00', NULL),
    (@v04, @uOlga,   @rIvan,   N'Отстой.',                                                                                                                                           1,  N'Rejected',  '2026-09-02T08:00:00', NULL),
    (@v05, @uAlex,   @rMaxim,  N'Обзор Cyberpunk 2077 на релизе был слишком резким, но по технической части во многом прав. Doom Eternal разобран отлично.',                     6,  N'Published', '2026-08-21T19:30:00', '2026-08-22T10:00:00'),
    (@v06, @uMaria,  @rAnna,   N'Лучшие разборы стратегий: объясняет баланс так, что хочется заново сыграть в Civilization VI.',                                                   10, N'Published', '2026-08-12T14:00:00', NULL),
    (@v07, @uDmitry, @rAnna,   N'В целом очень честный автор, хотя подкасты длинноваты.',                                                                                            8,  N'Published', '2026-09-05T20:10:00', NULL),
    (@v08, @uOlga,   @rKate,   N'Атмосферные разборы хорроров: Resident Evil 4 и Dead Space после её роликов воспринимаются по-новому.',                                           9,  N'Published', '2026-08-30T22:15:00', NULL),
    (@v09, @uAlex,   @rKate,   N'Статья про Hollow Knight — образец того, как писать о метроидваниях.',                                                                             8,  N'Pending',   '2026-10-24T11:00:00', NULL),
    (@v10, @uMaria,  @rMaxim,  N'Отличная подача, особенно в роликах про шутеры.',                                                                                                   8,  N'Published', '2026-09-10T16:45:00', NULL),
    (@v11, @uDmitry, @rSergey, N'Разбор баланса Civilization VI подробный, но с выводами про дополнения я не согласен.',                                                            6,  N'Published', '2026-09-18T09:30:00', NULL),
    (@v12, @uOlga,   @rNikita, N'Стримы уютные, но обзоры пока поверхностные — посмотрим, что будет дальше.',                                                                    7,  N'Pending',   '2026-10-25T19:00:00', '2026-10-26T08:30:00'),
    (@v13, @uIvan,   @rMaxim,  N'Как коллега: Максим умеет объяснить, почему шутер ощущается правильно. Иногда слишком категоричен.',                                              7,  N'Published', '2026-09-25T13:00:00', NULL);

-- Отзыв ссылается только на материалы того обзорщика, о котором он написан.
INSERT INTO ReviewMaterials (ReviewsId, MaterialsId) VALUES
    (@v01, @m01), (@v01, @m02),
    (@v02, @m03),
    (@v05, @m08), (@v05, @m09),
    (@v06, @m05), (@v06, @m07),
    (@v08, @m11), (@v08, @m12),
    (@v09, @m13),
    (@v10, @m08),
    (@v11, @m14),
    (@v13, @m10);

INSERT INTO ReviewCriterionScores (ReviewId, CriterionId, Score) VALUES
    (@v01, @cObjectivity, 9), (@v01, @cDepth, 10), (@v01, @cBuilds, 9),
    (@v02, @cObjectivity, 7), (@v02, @cDelivery, 8),
    (@v03, @cObjectivity, 3),
    (@v05, @cObjectivity, 5), (@v05, @cKnowledge, 7),
    (@v06, @cDepth, 10), (@v06, @cBalance, 10), (@v06, @cDelivery, 9),
    (@v07, @cObjectivity, 8),
    (@v08, @cAtmosphere, 10), (@v08, @cDelivery, 9),
    (@v09, @cDepth, 8),
    (@v10, @cDelivery, 9), (@v10, @cKnowledge, 8),
    (@v11, @cBalance, 6), (@v11, @cObjectivity, 7),
    (@v13, @cKnowledge, 8), (@v13, @cObjectivity, 6);

COMMIT TRANSACTION;

DECLARE @summary NVARCHAR(400) = CONCAT(
    N'Тестовые данные загружены. Тестовых пользователей ', (SELECT COUNT(*) FROM Users WHERE Email LIKE N'%@rereviewer.test'),
    N'; всего в базе: обзорщиков ', (SELECT COUNT(*) FROM Reviewers),
    N', игр ', (SELECT COUNT(*) FROM Games),
    N', материалов ', (SELECT COUNT(*) FROM Materials),
    N', отзывов ', (SELECT COUNT(*) FROM Reviews),
    N' (на модерации ', (SELECT COUNT(*) FROM Reviews WHERE Status = N'Pending'), N').');
PRINT @summary;