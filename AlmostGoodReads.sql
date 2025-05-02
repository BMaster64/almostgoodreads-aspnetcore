Create database AlmostGoodReads

-- 1. Users + Roles
CREATE TABLE Users (
    Id INT IDENTITY PRIMARY KEY,
    UserName NVARCHAR(256) NOT NULL UNIQUE,
    PasswordHash NVARCHAR(256) NOT NULL,
    Role NVARCHAR(50) NOT NULL
        CHECK (Role IN ('User','Admin'))
);

-- 2. Books
CREATE TABLE Books (
    Id INT IDENTITY PRIMARY KEY,
    Title NVARCHAR(255)    NOT NULL,
    Author NVARCHAR(255)   NOT NULL,
    Description NVARCHAR(MAX),
    GenreId INT
        REFERENCES Genres(Id)
        ON DELETE SET NULL,
    PublishYear INT,
    CoverImageUrl NVARCHAR(2083)
);

-- 3. Reviews
CREATE TABLE Reviews (
    Id INT IDENTITY PRIMARY KEY,
    BookId INT NOT NULL
        REFERENCES Books(Id)
        ON DELETE CASCADE,
    UserId INT NOT NULL
        REFERENCES Users(Id)
        ON DELETE CASCADE,
    Rating INT NOT NULL
        CHECK (Rating BETWEEN 1 AND 5),
    Comment NVARCHAR(MAX),
    CreatedAt DATETIME2 NOT NULL
        DEFAULT SYSUTCDATETIME()
);

-- 4. Genres
CREATE TABLE Genres (
    GenreId INT IDENTITY PRIMARY KEY,
    GenreName NVARCHAR(100) NOT NULL UNIQUE
);

-- 5. User Books (for tracking user book collections)
CREATE TABLE MyBooks (
    Id INT IDENTITY PRIMARY KEY,
    UserId INT NOT NULL
        REFERENCES Users(Id)
        ON DELETE CASCADE,
    BookId INT NOT NULL
        REFERENCES Books(Id)
        ON DELETE CASCADE,
    Status INT NOT NULL
        CHECK (Status BETWEEN 1 AND 4),
    -- 1: Plan to read, 2: Currently reading, 3: Dropped, 4: Completed
    DateAdded DATETIME2 NOT NULL
        DEFAULT SYSUTCDATETIME(),
    CONSTRAINT UQ_UserBook UNIQUE (UserId, BookId)
);

-- 6. BookGenres
ALTER TABLE Books
DROP CONSTRAINT FK__Books__GenreId__628FA481;

ALTER TABLE Books
DROP COLUMN GenreId;

CREATE TABLE BookGenres (
    BookId INT NOT NULL
        REFERENCES Books(Id)
        ON DELETE CASCADE,
    GenreId INT NOT NULL
        REFERENCES Genres(GenreId)
        ON DELETE CASCADE,
    PRIMARY KEY (BookId, GenreId)
);

-- ====================================
-- Example CRUD queries for each feature
-- ====================================

-- A. Register a new user (hashed password inserted by your app)
INSERT INTO Users (UserName, PasswordHash, Role)
VALUES ('alice@example.com', 'AQB...hashed...', 'User');

-- B. Add a new book (Admin only)
INSERT INTO Books (Title, Author, Description, Genre, PublishYear, CoverImageUrl)
VALUES (
  'The Great Gatsby',
  'F. Scott Fitzgerald',
  'A portrait of the Jazz Age in all of its decadence…',
  'Classic',
  1925,
  'https://…/gatsby.jpg'
);

-- C. List & search books
--    – All:
SELECT * FROM Books;
--    – Filter by title substring or genre:
SELECT * FROM Books
 WHERE Title   LIKE '%gatsby%'
    OR Genre   = 'Classic';

-- D. Edit a book (Admin only)
UPDATE Books
   SET Description   = 'New description…',
       PublishYear   = 1926
 WHERE Id = 1;

-- E. Delete a book (Admin only)
DELETE FROM Books
 WHERE Id = 1;



-- G. Edit own review
UPDATE Reviews
   SET Rating  = 4,
       Comment = 'Actually, 4 stars—great but a bit slow in parts.'
 WHERE Id     = 3
   AND UserId = 1;   -- ensure user can only edit their own

-- H. Delete a review (User or Admin)
--    – User deleting their own:
DELETE FROM Reviews
 WHERE Id     = 3
   AND UserId = 1;
--    – Admin deleting any:
DELETE FROM Reviews
 WHERE Id = 3;

-- I. (Optional) Like/dislike or report would require extra tables:
-- CREATE TABLE ReviewVotes ( … ), CREATE TABLE ReviewReports ( … )
