// InputHandler.cs — Optional pointer normalizer for board input
// Ported from src/game/InputHandler.ts

using RoyalMatch.Types;

namespace RoyalMatch.Match3
{
    public enum InputPhase
    {
        IDLE,
        SELECTING,
        DRAGGING,
        SWAPPING,
        LOCKED,
    }

    public enum InputActionType
    {
        Select,
        Swap,
        SwapBack,
    }

    public class InputAction
    {
        public InputActionType Type;
        public Point? From;
        public Point? To;
        public bool Valid;

        public InputAction(InputActionType type, Point? from, Point? to, bool valid)
        {
            Type = type;
            From = from;
            To = to;
            Valid = valid;
        }
    }

    public class InputHandler
    {
        private InputPhase _phase = InputPhase.IDLE;
        private Point? _selectedCell = null;
        private Point? _dragStart = null;
        private Point? _dragCurrent = null;
        private bool _locked = false;

        public InputPhase GetPhase() => _phase;
        public Point? GetSelectedCell() => _selectedCell;

        public void Reset()
        {
            _phase = InputPhase.IDLE;
            _selectedCell = null;
            _dragStart = null;
            _dragCurrent = null;
            _locked = false;
        }

        public void Lock()
        {
            _locked = true;
            _phase = InputPhase.LOCKED;
        }

        public void Unlock()
        {
            _locked = false;
            _phase = InputPhase.IDLE;
            _selectedCell = null;
        }

        public InputAction HandlePointerDown(int row, int col)
        {
            if (_locked) return null;

            if (_phase == InputPhase.IDLE || _phase == InputPhase.SELECTING)
            {
                if (_selectedCell.HasValue && IsAdjacent(row, col, _selectedCell.Value))
                {
                    _selectedCell = new Point(row, col);
                    _phase = InputPhase.SELECTING;
                    return new InputAction(InputActionType.Select, _selectedCell, new Point(row, col), true);
                }
                _selectedCell = new Point(row, col);
                _phase = InputPhase.SELECTING;
                return new InputAction(InputActionType.Select, null, new Point(row, col), true);
            }

            return null;
        }

        public InputAction HandlePointerUp(int row, int col)
        {
            if (_locked) return null;
            if (!_selectedCell.HasValue) return null;

            if (IsAdjacent(row, col, _selectedCell.Value))
            {
                var action = new InputAction(InputActionType.Swap, _selectedCell, new Point(row, col), true);
                _phase = InputPhase.SWAPPING;
                return action;
            }

            return null;
        }

        public InputAction HandlePointerMove(int row, int col)
        {
            if (_locked) return null;
            if (!_selectedCell.HasValue) return null;
            if (_phase != InputPhase.SELECTING && _phase != InputPhase.DRAGGING) return null;

            if (IsAdjacent(row, col, _selectedCell.Value) && !_dragStart.HasValue)
            {
                _dragStart = _selectedCell;
                _dragCurrent = new Point(row, col);
                _phase = InputPhase.DRAGGING;
                return new InputAction(InputActionType.Swap, _selectedCell, new Point(row, col), true);
            }

            return null;
        }

        public void CancelSwap()
        {
            if (_phase == InputPhase.SWAPPING && _selectedCell.HasValue)
                _phase = InputPhase.SELECTING;
        }

        public void ConfirmSwap()
        {
            _phase = InputPhase.LOCKED;
        }

        private bool IsAdjacent(int r1, int c1, Point p2)
        {
            return (System.Math.Abs(r1 - p2.Row) == 1 && c1 == p2.Col) ||
                   (System.Math.Abs(c1 - p2.Col) == 1 && r1 == p2.Row);
        }
    }
}
