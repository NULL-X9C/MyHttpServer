CREATE TABLE IF NOT EXISTS users
(
    id    SERIAL PRIMARY KEY,
    name  VARCHAR(100) NOT NULL,
    email VARCHAR(150) NOT NULL,
    age   INT          NOT NULL
);

CREATE TABLE IF NOT EXISTS products
(
    id      SERIAL PRIMARY KEY,
    title   VARCHAR(150)   NOT NULL,
    price   DECIMAL(10, 2) NOT NULL,
    instock BOOLEAN        NOT NULL
);