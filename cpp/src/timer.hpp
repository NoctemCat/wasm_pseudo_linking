#pragma once
#include <chrono>
#include <cstdlib>

class Timer {

public:
    Timer(bool startOnCreate = false) {
        if (startOnCreate) {
            start();
        }
    }

    void start() { beg = std::chrono::high_resolution_clock::now(); }

    double stop() {
        end = std::chrono::high_resolution_clock::now();
        return count_ms_double();
    }

    long long count_ms_long() const {
        return std::chrono::duration_cast<std::chrono::milliseconds>(end - beg).count();
    }

    double count_ms_double() const {
        return std::chrono::duration<double, std::milli>(end - beg).count();
    }

    class ScopeGuard {
    public:
        ScopeGuard(Timer *in_timer) : timer(in_timer) { timer->start(); }

        ~ScopeGuard() { timer->stop(); }

    private:
        Timer *timer;
    };

    ScopeGuard MeasureScope() { return ScopeGuard(this); }

private:
    std::chrono::high_resolution_clock::time_point beg;
    std::chrono::high_resolution_clock::time_point end;
};