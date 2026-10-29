/*
    Удаляет тестовые данные, загруженные SeedTestData.sql. Только для разработки.

        sqlcmd -S localhost -E -C -d ReReviewerDB -f 65001 -i ReReviewer.Data/Scripts/DeleteTestData.sql

    Что удаляется:
        - пользователи с почтой @rereviewer.test и всё, что они написали, — в том числе созданное
          позже через API (отзывы вместе с баллами и ссылками на материалы, роли пользователей);
        - тестовые обзорщики, издания и игры (по фиксированным Id из SeedTestData.sql),
          все материалы тестовых обзорщиков и все отзывы о них, кто бы их ни написал;
        - роли, жанры, бейджи и критерии из SeedTestData.sql — только если на них больше ничего
          не ссылается. Иначе удаление каскадом сняло бы, например, роль Admin с вашего аккаунта.

    Что остаётся:
        - тестовая игра, если на неё ссылается ваш материал (удаление запрещено ограничением базы);
        - у ваших материалов, опубликованных в тестовом издании, издание просто сбрасывается.

    Всё выполняется в одной транзакции. Повторный запуск безопасен: удалять будет нечего.
*/

-- sqlcmd по умолчанию выключает QUOTED_IDENTIFIER, а без него SQL Server не даёт менять таблицы
-- с фильтрованными индексами (Reviewers.UserId). Отдельный пакет — чтобы настройки действовали на весь скрипт.
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @testReviewers TABLE (Id UNIQUEIDENTIFIER PRIMARY KEY);
INSERT INTO @testReviewers (Id) VALUES
    ('22222222-0000-0000-0000-000000000001'), ('22222222-0000-0000-0000-000000000002'),
    ('22222222-0000-0000-0000-000000000003'), ('22222222-0000-0000-0000-000000000004'),
    ('22222222-0000-0000-0000-000000000005'), ('22222222-0000-0000-0000-000000000006');

DECLARE @testOutlets TABLE (Id UNIQUEIDENTIFIER PRIMARY KEY);
INSERT INTO @testOutlets (Id) VALUES
    ('33333333-0000-0000-0000-000000000001'), ('33333333-0000-0000-0000-000000000002'),
    ('33333333-0000-0000-0000-000000000003');

DECLARE @testGames TABLE (Id UNIQUEIDENTIFIER PRIMARY KEY);
INSERT INTO @testGames (Id) VALUES
    ('44444444-0000-0000-0000-000000000001'), ('44444444-0000-0000-0000-000000000002'),
    ('44444444-0000-0000-0000-000000000003'), ('44444444-0000-0000-0000-000000000004'),
    ('44444444-0000-0000-0000-000000000005'), ('44444444-0000-0000-0000-000000000006'),
    ('44444444-0000-0000-0000-000000000007'), ('44444444-0000-0000-0000-000000000008'),
    ('44444444-0000-0000-0000-000000000009'), ('44444444-0000-0000-0000-000000000010');

DECLARE @testUsers TABLE (Id UNIQUEIDENTIFIER PRIMARY KEY);
INSERT INTO @testUsers (Id)
SELECT Id FROM Users WHERE Email LIKE N'%@rereviewer.test';

DECLARE @reviews INT, @materials INT, @reviewers INT, @outlets INT, @games INT, @keptGames INT, @users INT,
        @criteria INT, @badges INT, @genres INT, @roles INT;

BEGIN TRANSACTION;

-----------------------------------------------------------------------------------------------
-- Отзывы, материалы, обзорщики, издания, игры, пользователи. Порядок важен: базу не даёт удалить
-- обзорщика, у которого есть отзывы или материалы, и игру, у которой есть материалы.
-----------------------------------------------------------------------------------------------

-- Баллы по критериям и ссылки на материалы удаляются вместе с отзывами каскадом.
DELETE FROM Reviews
WHERE ReviewerId IN (SELECT Id FROM @testReviewers)
   OR UserId IN (SELECT Id FROM @testUsers);
SET @reviews = @@ROWCOUNT;

DELETE FROM Materials
WHERE ReviewerId IN (SELECT Id FROM @testReviewers);
SET @materials = @@ROWCOUNT;

-- Жанры, издания и бейджи обзорщиков удаляются каскадом.
DELETE FROM Reviewers
WHERE Id IN (SELECT Id FROM @testReviewers);
SET @reviewers = @@ROWCOUNT;

-- У ваших материалов из этих изданий OutletId сбрасывается в NULL.
DELETE FROM Outlets
WHERE Id IN (SELECT Id FROM @testOutlets);
SET @outlets = @@ROWCOUNT;

DELETE g FROM Games g
WHERE g.Id IN (SELECT Id FROM @testGames)
  AND NOT EXISTS (SELECT 1 FROM Materials m WHERE m.GameId = g.Id);
SET @games = @@ROWCOUNT;

SET @keptGames = (SELECT COUNT(*) FROM Games WHERE Id IN (SELECT Id FROM @testGames));

-- Роли пользователей удаляются каскадом; если к тестовому аккаунту был привязан ваш обзорщик, привязка сбрасывается.
DELETE FROM Users
WHERE Id IN (SELECT Id FROM @testUsers);
SET @users = @@ROWCOUNT;

-----------------------------------------------------------------------------------------------
-- Справочники из SeedTestData.sql — только те записи, на которые больше ничего не ссылается.
-----------------------------------------------------------------------------------------------

DELETE c FROM Criteria c
JOIN (VALUES
    (N'Объективность', NULL),
    (N'Глубина анализа', NULL),
    (N'Подача', NULL),
    (N'Знание игры', NULL),
    (N'Разбор билдов', N'RPG'),
    (N'Анализ баланса', N'Strategy'),
    (N'Атмосфера и напряжение', N'Horror')
) AS v(Title, GenreTitle) ON v.Title = c.Title
LEFT JOIN Genres g ON g.Title = v.GenreTitle
WHERE ((v.GenreTitle IS NULL AND c.GenreId IS NULL) OR c.GenreId = g.Id)
  AND NOT EXISTS (SELECT 1 FROM ReviewCriterionScores s WHERE s.CriterionId = c.Id);
SET @criteria = @@ROWCOUNT;

DELETE b FROM Badges b
WHERE b.Title IN (N'Объективный', N'Глубокий разбор', N'Без спойлеров', N'Ветеран индустрии')
  AND NOT EXISTS (SELECT 1 FROM ReviewerBadges rb WHERE rb.BadgeId = b.Id);
SET @badges = @@ROWCOUNT;

DELETE g FROM Genres g
WHERE g.Title IN (N'RPG', N'Action', N'Shooter', N'Strategy', N'Indie', N'Horror')
  AND NOT EXISTS (SELECT 1 FROM Criteria c WHERE c.GenreId = g.Id)
  AND NOT EXISTS (SELECT 1 FROM GameGenres gg WHERE gg.GenresId = g.Id)
  AND NOT EXISTS (SELECT 1 FROM ReviewerGenres rg WHERE rg.GenresId = g.Id);
SET @genres = @@ROWCOUNT;

DELETE r FROM Roles r
WHERE r.Title IN (N'Admin', N'Moderator')
  AND NOT EXISTS (SELECT 1 FROM UserRoles ur WHERE ur.RolesId = r.Id);
SET @roles = @@ROWCOUNT;

COMMIT TRANSACTION;

DECLARE @summary NVARCHAR(600) =
    CASE WHEN @reviews + @materials + @reviewers + @outlets + @games + @users + @criteria + @badges + @genres + @roles = 0
        THEN N'Тестовых данных не найдено, ничего не удалено.'
        ELSE CONCAT(
            N'Удалено: пользователей ', @users, N', обзорщиков ', @reviewers, N', изданий ', @outlets,
            N', игр ', @games, N', материалов ', @materials, N', отзывов ', @reviews,
            N', критериев ', @criteria, N', бейджей ', @badges, N', жанров ', @genres, N', ролей ', @roles, N'.',
            CASE WHEN @keptGames > 0 THEN CONCAT(N' Оставлено игр: ', @keptGames, N' — на них ссылаются ваши материалы.') ELSE N'' END)
    END;
PRINT @summary;