// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

document.addEventListener("DOMContentLoaded", () => {
    const paletteToggle = document.querySelector("#palette-toggle");
    const palettePopover = document.querySelector("#palette-popover");
    const paletteMenu = document.querySelector("#palette-menu");
    const boardColorsOption = document.querySelector("#palette-board-colors");
    const paletteOptions = document.querySelector("#palette-options");

    if (paletteToggle && palettePopover && paletteMenu && boardColorsOption && paletteOptions) {
        const storageKey = "chess-board-palette";
        const palettes = Array.from({ length: 100 }, (_, index) => {
            const hue = Math.round(index * 137.508 % 360);
            const saturation = 42 + (index * 37 % 49);
            const lightness = 76 + (index * 17 % 17);
            const darkness = 24 + (index * 23 % 21);
            return {
                light: `hsl(${hue} ${saturation}% ${lightness}%)`,
                dark: `hsl(${hue} ${saturation}% ${darkness}%)`
            };
        });
        let selectedPalette = -1;

        function setPalette(index, persist) {
            const palette = palettes[index];
            if (!palette) {
                return;
            }

            selectedPalette = index;
            document.documentElement.style.setProperty("--board-light-square", palette.light);
            document.documentElement.style.setProperty("--board-dark-square", palette.dark);
            paletteOptions.querySelectorAll(".palette-option").forEach((option, optionIndex) => {
                option.setAttribute("aria-pressed", String(optionIndex === index));
                option.tabIndex = optionIndex === index ? 0 : -1;
            });

            if (persist) {
                try {
                    localStorage.setItem(storageKey, String(index));
                } catch {
                }
            }
        }

        palettes.forEach((palette, index) => {
            const option = document.createElement("button");
            option.type = "button";
            option.className = "palette-option";
            option.setAttribute("aria-label", `Palette ${String(index + 1).padStart(2, "0")}, light and dark board colors`);
            option.setAttribute("aria-pressed", "false");
            option.tabIndex = index === 0 ? 0 : -1;
            option.style.setProperty("--palette-light", palette.light);
            option.style.setProperty("--palette-dark", palette.dark);
            option.addEventListener("click", () => {
                setPalette(index, true);
                palettePopover.hidden = true;
                paletteToggle.setAttribute("aria-expanded", "false");
                paletteToggle.focus();
            });
            paletteOptions.append(option);
        });

        let savedPalette;
        try {
            savedPalette = Number.parseInt(localStorage.getItem(storageKey), 10);
        } catch {
            savedPalette = -1;
        }
        if (Number.isInteger(savedPalette) && savedPalette >= 0 && savedPalette < palettes.length) {
            setPalette(savedPalette, false);
        }

        boardColorsOption.addEventListener("click", () => {
            paletteMenu.hidden = true;
            paletteOptions.hidden = false;
            paletteOptions.querySelector(".palette-option[tabindex='0']")?.focus();
        });

        paletteToggle.addEventListener("click", () => {
            palettePopover.hidden = !palettePopover.hidden;
            paletteToggle.setAttribute("aria-expanded", String(!palettePopover.hidden));
            if (!palettePopover.hidden) {
                paletteMenu.hidden = false;
                paletteOptions.hidden = true;
                boardColorsOption.focus();
            }
        });

        paletteOptions.addEventListener("keydown", event => {
            const option = event.target.closest(".palette-option");
            if (!option) {
                return;
            }

            const currentIndex = Array.from(paletteOptions.children).indexOf(option);
            const moves = { ArrowRight: 1, ArrowLeft: -1, ArrowDown: 2, ArrowUp: -2 };
            if (!(event.key in moves)) {
                return;
            }

            event.preventDefault();
            const nextIndex = Math.max(0, Math.min(palettes.length - 1, currentIndex + moves[event.key]));
            const nextOption = paletteOptions.children[nextIndex];
            option.tabIndex = -1;
            nextOption.tabIndex = 0;
            nextOption.focus();
        });

        document.addEventListener("click", event => {
            if (!palettePopover.hidden && !palettePopover.contains(event.target) && !paletteToggle.contains(event.target)) {
                palettePopover.hidden = true;
                paletteToggle.setAttribute("aria-expanded", "false");
            }
        });

        document.addEventListener("keydown", event => {
            if (event.key === "Escape" && !palettePopover.hidden) {
                palettePopover.hidden = true;
                paletteToggle.setAttribute("aria-expanded", "false");
                paletteToggle.focus();
            }
        });
    }

    const board = document.querySelector(".board");
    if (!board) {
        return;
    }

    const status = document.querySelector("#board-status");
    const gameResult = document.querySelector("#game-result");
    const promotionPicker = document.querySelector("#promotion-picker");
    const resetButton = document.querySelector("#reset-board");
    const moveHistoryElement = document.querySelector("#move-history");
    const emptyHistoryElement = document.querySelector("#history-empty");
    const principalVariationsElement = document.querySelector("#principal-variations");
    const evaluationBar = document.querySelector("#evaluation-bar");
    const evaluationBarWhite = document.querySelector("#evaluation-bar-white");
    const evaluationScore = document.querySelector("#evaluation-score");
    const pieceNames = { p: "pawn", n: "knight", b: "bishop", r: "rook", q: "queen", k: "king" };
    const promotionValues = { queen: 2, knight: 4, bishop: 5, rook: 6 };
    let fen = board.dataset.fen;
    let gameOver = board.dataset.isCheckmate === "True" || board.dataset.isCheckmate === "true"
        || board.dataset.isStalemate === "True" || board.dataset.isStalemate === "true";
    let selectedSquare = null;
    let legalMoves = [];
    let legalMovesPromise = Promise.resolve([]);
    let selectionVersion = 0;
    let pendingPromotion = null;
    let suppressClickUntil = 0;
    let moveInProgress = false;
    let moveHistory = [];
    let evaluationRequestVersion = 0;

    function currentSideColor() {
        return fen.split(" ")[1] === "w" ? "white" : "black";
    }

    function formatEvaluation(centipawns) {
        return Math.abs(centipawns) >= 99000
            ? `${centipawns > 0 ? "White" : "Black"} mates`
            : `${centipawns > 0 ? "+" : ""}${(centipawns / 100).toFixed(2)}`;
    }

    async function updateEvaluation() {
        const requestVersion = ++evaluationRequestVersion;
        const requestedFen = fen;
        evaluationScore.textContent = "Evaluating…";
        principalVariationsElement.replaceChildren();
        try {
            const response = await fetch(board.dataset.evaluateUrl, {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({ fen: requestedFen })
            });
            const result = await response.json();
            if (!response.ok) {
                throw new Error(result.error || "Evaluation unavailable");
            }

            if (requestVersion !== evaluationRequestVersion || requestedFen !== fen) {
                return;
            }

            const centipawns = result.centipawns;
            const whiteShare = Math.max(5, Math.min(95, 50 + Math.tanh(centipawns / 600) * 45));
            evaluationBar.setAttribute("aria-valuenow", String(Math.round(whiteShare)));
            const scoreText = formatEvaluation(centipawns);
            evaluationBarWhite.style.width = `${whiteShare}%`;
            evaluationBar.setAttribute("aria-valuetext", `${scoreText}, depth ${result.depth}`);
            evaluationScore.textContent = `${scoreText} · depth ${result.depth}`;

            for (const [index, line] of (result.lines || []).entries()) {
                const item = document.createElement("li");
                item.className = "principal-variation";

                const score = document.createElement("span");
                score.className = "principal-variation-score";
                score.textContent = formatEvaluation(line.centipawns);

                const moves = document.createElement("span");
                moves.className = "principal-variation-moves";
                moves.textContent = line.moves.join(" ");
                item.setAttribute("aria-label", `Line ${index + 1}: ${score.textContent} ${moves.textContent}`);
                item.append(score, moves);
                principalVariationsElement.append(item);
            }
        } catch {
            if (requestVersion === evaluationRequestVersion && requestedFen === fen) {
                evaluationScore.textContent = "Unavailable";
                evaluationBar.setAttribute("aria-valuetext", "Evaluation unavailable");
                principalVariationsElement.replaceChildren();
            }
        }
    }

    function setStatus(message) {
        status.textContent = message;
    }

    function setTurnStatus() {
        const side = document.createElement("strong");
        side.textContent = currentSideColor() === "white" ? "White" : "Black";
        status.replaceChildren(side, document.createTextNode(" to move"));
    }

    function renderHistory() {
        moveHistoryElement.replaceChildren();
        const rows = new Map();
        for (const move of moveHistory) {
            let row = rows.get(move.moveNumber);
            if (!row) {
                row = { moveNumber: move.moveNumber, whiteSan: "", blackSan: "" };
                rows.set(move.moveNumber, row);
            }

            if (move.isWhiteMove) {
                row.whiteSan = move.san;
            } else {
                row.blackSan = move.san;
            }
        }

        emptyHistoryElement.hidden = rows.size > 0;
        for (const row of rows.values()) {
            const item = document.createElement("li");
            item.className = "move-entry";

            const number = document.createElement("span");
            number.className = "move-number";
            number.textContent = `${row.moveNumber}${row.whiteSan ? "." : "..."}`;

            const whiteMove = document.createElement("span");
            whiteMove.textContent = row.whiteSan;
            const blackMove = document.createElement("span");
            blackMove.textContent = row.blackSan;

            item.append(number, whiteMove, blackMove);
            moveHistoryElement.append(item);
        }
    }

    function hidePromotionPicker() {
        promotionPicker.hidden = true;
        pendingPromotion = null;
    }

    function clearLegalHighlights() {
        board.querySelectorAll(".legal-move, .legal-capture").forEach(square => {
            square.classList.remove("legal-move", "legal-capture");
        });
    }

    function clearSelection() {
        selectionVersion++;
        selectedSquare?.classList.remove("selected");
        selectedSquare = null;
        legalMoves = [];
        legalMovesPromise = Promise.resolve([]);
        clearLegalHighlights();
        hidePromotionPicker();
    }

    function findSquare(rank, file) {
        return Array.from(board.querySelectorAll(".square")).find(square =>
            Number(square.dataset.rank) === rank && Number(square.dataset.file) === file);
    }

    function isTrue(value) {
        return value === true || value === "true" || value === "True";
    }

    function updateGameState(isCheck, isCheckmate, isStalemate, checkedKingRank, checkedKingFile) {
        const check = isTrue(isCheck);
        const checkmate = isTrue(isCheckmate);
        const stalemate = isTrue(isStalemate);
        board.querySelectorAll(".in-check").forEach(square => square.classList.remove("in-check"));

        const rank = Number(checkedKingRank);
        const file = Number(checkedKingFile);
        if (check && rank >= 1 && rank <= 8 && file >= 1 && file <= 8) {
            findSquare(rank, file)?.classList.add("in-check");
        }

        board.dataset.isCheck = String(check);
        board.dataset.isCheckmate = String(checkmate);
        board.dataset.isStalemate = String(stalemate);
        board.dataset.checkRank = check ? String(rank) : "";
        board.dataset.checkFile = check ? String(file) : "";
        gameOver = checkmate || stalemate;
        gameResult.hidden = !gameOver;
        if (checkmate) {
            gameResult.textContent = `Checkmate! ${currentSideColor() === "white" ? "Black" : "White"} wins.`;
        } else if (stalemate) {
            gameResult.textContent = "Stalemate. The game is over.";
        } else {
            gameResult.textContent = "";
        }
    }

    async function selectSquare(square) {
        if (gameOver) {
            return;
        }

        const piece = square.querySelector(".piece");
        if (!piece || piece.dataset.color !== currentSideColor()) {
            return;
        }

        clearSelection();
        selectedSquare = square;
        selectedSquare.classList.add("selected");
        setTurnStatus();
        const requestVersion = selectionVersion;
        legalMovesPromise = (async () => {
            try {
                const response = await fetch(board.dataset.legalMovesUrl, {
                    method: "POST",
                    headers: { "Content-Type": "application/json" },
                    body: JSON.stringify({ fen, rank: Number(square.dataset.rank), file: Number(square.dataset.file) })
                });
                const moves = await response.json();
                if (!response.ok) {
                    throw new Error(moves.error || "Unable to get legal moves.");
                }

                if (requestVersion !== selectionVersion || selectedSquare !== square) {
                    return [];
                }

                legalMoves = moves;
                for (const move of legalMoves) {
                    const destination = findSquare(move.rank, move.file);
                    if (destination) {
                        destination.classList.add(move.isCapture ? "legal-capture" : "legal-move");
                    }
                }

                setTurnStatus();
                return legalMoves;
            } catch (error) {
                if (requestVersion === selectionVersion) {
                    setStatus(error.message);
                }
                return [];
            }
        })();

        await legalMovesPromise;
    }

    function renderPosition() {
        const ranks = fen.split(" ")[0].split("/");
        board.querySelectorAll(".square").forEach(square => square.replaceChildren());
        for (let rankIndex = 0; rankIndex < ranks.length; rankIndex++) {
            let file = 1;
            const rank = 8 - rankIndex;
            for (const symbol of ranks[rankIndex]) {
                if (Number.isInteger(Number(symbol))) {
                    file += Number(symbol);
                    continue;
                }

                const square = findSquare(rank, file++);
                const color = symbol === symbol.toUpperCase() ? "white" : "black";
                const pieceName = pieceNames[symbol.toLowerCase()];
                const image = document.createElement("img");
                image.src = `${board.dataset.imageBaseUrl}${color}_${pieceName}.svg`;
                image.alt = `${color} ${pieceName}`;
                image.className = "piece";
                image.dataset.color = color;
                image.draggable = true;
                square.replaceChildren(image);
            }
        }

        for (const square of board.querySelectorAll(".square")) {
            if (!square.querySelector(".piece")) {
                square.replaceChildren();
            }
        }

        board.dataset.fen = fen;
        setTurnStatus();
    }

    async function submitMove(destination, promotion = null) {
        if (!selectedSquare || moveInProgress || gameOver) {
            return;
        }

        moveInProgress = true;
        const source = selectedSquare;
        try {
            const response = await fetch(board.dataset.makeMoveUrl, {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({
                    fen,
                    startRank: Number(source.dataset.rank),
                    startFile: Number(source.dataset.file),
                    endRank: Number(destination.dataset.rank),
                    endFile: Number(destination.dataset.file),
                    promotion: promotion ? promotionValues[promotion] : null
                })
            });
            const result = await response.json();
            if (!response.ok) {
                throw new Error(result.error || "The move was rejected.");
            }

            fen = result.fen;
            moveHistory.push({
                moveNumber: result.moveNumber,
                isWhiteMove: result.isWhiteMove,
                san: result.san
            });
            renderHistory();
            clearSelection();
            board.querySelectorAll(".last-moved").forEach(square => square.classList.remove("last-moved"));
            renderPosition();
            findSquare(Number(destination.dataset.rank), Number(destination.dataset.file))?.classList.add("last-moved");
            updateGameState(result.isCheck, result.isCheckmate, result.isStalemate, result.checkedKingRank, result.checkedKingFile);
            void updateEvaluation();
        } catch (error) {
            hidePromotionPicker();
            setStatus(error.message);
        } finally {
            moveInProgress = false;
            resetButton.disabled = false;
        }
    }

    async function moveTo(destination) {
        if (gameOver) {
            return;
        }

        const source = selectedSquare;
        const movesPromise = legalMovesPromise;
        const moves = await movesPromise;
        if (!source || selectedSquare !== source) {
            return;
        }

        const destinationMoves = moves.filter(move =>
            move.rank === Number(destination.dataset.rank) && move.file === Number(destination.dataset.file));
        if (destinationMoves.length === 0) {
            return;
        }

        const promotions = destinationMoves.filter(move => move.promotion).map(move => move.promotion);
        if (promotions.length > 0) {
            pendingPromotion = { destination, promotions };
            promotionPicker.hidden = false;
            setStatus("Choose a promotion piece.");
            return;
        }

        await submitMove(destination);
    }

    board.addEventListener("click", async event => {
        if (gameOver || moveInProgress || performance.now() < suppressClickUntil) {
            return;
        }

        const square = event.target.closest(".square");
        if (!square || !board.contains(square)) {
            return;
        }

        const piece = square.querySelector(".piece");
        if (!selectedSquare) {
            await selectSquare(square);
            return;
        }

        if (square === selectedSquare) {
            clearSelection();
            setTurnStatus();
            return;
        }

        if (piece && piece.dataset.color === currentSideColor()) {
            await selectSquare(square);
            return;
        }

        await moveTo(square);
    });

    board.addEventListener("dragstart", event => {
        if (gameOver || moveInProgress) {
            event.preventDefault();
            return;
        }
        const piece = event.target.closest(".piece");
        const square = piece?.closest(".square");
        if (!piece || !square || piece.dataset.color !== currentSideColor()) {
            event.preventDefault();
            return;
        }

        void selectSquare(square);
        event.dataTransfer.effectAllowed = "move";
        event.dataTransfer.setData("text/plain", `${square.dataset.rank},${square.dataset.file}`);
    });

    board.addEventListener("dragover", event => {
        if (!gameOver && selectedSquare && event.target.closest(".square")) {
            event.preventDefault();
            event.dataTransfer.dropEffect = "move";
        }
    });

    board.addEventListener("drop", async event => {
        const destination = event.target.closest(".square");
        if (gameOver || !destination || !selectedSquare) {
            return;
        }

        event.preventDefault();
        suppressClickUntil = performance.now() + 250;
        await moveTo(destination);
    });

    promotionPicker.addEventListener("click", async event => {
        const button = event.target.closest("[data-promotion]");
        if (!button || !pendingPromotion) {
            return;
        }

        if (button.dataset.promotion === "cancel") {
            hidePromotionPicker();
            return;
        }

        const promotion = button.dataset.promotion;
        if (!pendingPromotion.promotions.includes(promotion)) {
            return;
        }

        const destination = pendingPromotion.destination;
        hidePromotionPicker();
        await submitMove(destination, promotion);
    });

    resetButton.addEventListener("click", () => {
        if (moveInProgress) {
            return;
        }

        clearSelection();
        fen = board.dataset.startingFen;
        moveHistory = [];
        board.querySelectorAll(".last-moved").forEach(square => square.classList.remove("last-moved"));
        renderPosition();
        renderHistory();
        updateGameState(board.dataset.startingIsCheck, board.dataset.startingIsCheckmate, board.dataset.startingIsStalemate,
            board.dataset.startingCheckRank, board.dataset.startingCheckFile);
        void updateEvaluation();
    });

    updateGameState(board.dataset.isCheck, board.dataset.isCheckmate, board.dataset.isStalemate,
        board.dataset.checkRank, board.dataset.checkFile);
    renderHistory();
    void updateEvaluation();
});
